using System.Collections.Generic;
using UnityEngine;

// 无限赛道的生成器。
// 赛道由若干块“圆柱网格”组成，车辆基本停在原地，地块带着自己前进；
// 每块用柏林噪声做起伏，并在接缝处向上一块的末排顶点插值，
// 保证两块地形之间没有裂缝。旧地块跑出身后就销毁，前方再补一块，
// 所以可以一直跑下去。
public class WorldGenerator : MonoBehaviour
{
    public Material meshMaterial;   // 赛道材质
    public float scale;             // 单个网格的边长
    public float perlinScale;       // 噪声采样密度
    public float offset;            // 噪声采样偏移，数值变化地形就跟着变
    public Vector2 dimensions;      // 一块地横竖各多少格
    public float waveHeight;        // 地形起伏高度
    public float globalSpeed = 30f; // 地块前进速度

    // 接缝处向前多少排做平滑过渡
    public int startTransitionLength = 5;
    // 每生成一块地形，噪声 offset 前移的量，让地形持续变化
    public float randomness = 10f;

    public int startObstacleChance; // 每个顶点生成障碍的概率分母
    public int gateChance;          // 障碍里出现门的概率分母
    public GameObject[] obstacles;  // 可选障碍
    public GameObject gate;         // 金色门框

    private GameObject[] pieces = new GameObject[2]; // 同一时间存活的两块地形
    private float pieceLength;                       // 一块地的长度
    private Vector3[] beginPoints;                   // 上一块末排顶点（接缝过渡用）
    private GameObject currentPiece;                 // 正在生成的那块地

    private void Start()
    {
        int zCount = (int)dimensions.y;
        pieceLength = zCount * scale * Mathf.PI;

        // 先铺好两块，之后靠回收循环一直补新的
        for (int i = 0; i < pieces.Length; i++)
        {
            GenerateWorldPiece(i);
        }
    }

    private void LateUpdate()
    {
        // 靠后的那块都到 -7 了，说明整圈已经跑完，可以开始回收
        if (pieces[1] != null && pieces[1].transform.position.z <= -7f)
        {
            RecycleWorld();
        }
    }

    private void RecycleWorld()
    {
        Destroy(pieces[0]);
        pieces[0] = pieces[1];

        // 在剩余那块的前方补一块新的，首尾长度保持一致
        float newWorldZ = pieces[0].transform.position.z + pieceLength;
        pieces[1] = CreateCylinder();
        pieces[1].transform.position = new Vector3(0f, 0f, newWorldZ);
        pieces[1].transform.rotation = pieces[0].transform.rotation;

        UpdateSinglePiece(pieces[1]);
    }

    private void GenerateWorldPiece(int index)
    {
        float worldZ = index * pieceLength;
        pieces[index] = CreateCylinder();
        pieces[index].transform.position = new Vector3(0f, 0f, worldZ);
        UpdateSinglePiece(pieces[index]);
    }

    // 给一块地装上移动组件，并在末端放一个标记
    private void UpdateSinglePiece(GameObject piece)
    {
        BasicMovement movement = piece.AddComponent<BasicMovement>();
        movement.movespeed = globalSpeed;

        GameObject endPoint = new GameObject("End Point");
        endPoint.transform.position = piece.transform.position + Vector3.forward * pieceLength;
        endPoint.transform.SetParent(piece.transform, true);

        // 换一块地就挪一下噪声采样起点，保证前后地形不一样
        offset += randomness;
    }

    private GameObject CreateCylinder()
    {
        GameObject go = new GameObject("World piece");
        currentPiece = go;

        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = meshMaterial;
        meshFilter.mesh = GenerateMesh();

        go.AddComponent<MeshCollider>();
        return go;
    }

    private Mesh GenerateMesh()
    {
        int xCount = (int)dimensions.x;
        int zCount = (int)dimensions.y;
        float radius = xCount * scale * 0.5f;

        List<Vector3> vertices = new List<Vector3>((xCount + 1) * (zCount + 1));
        List<Vector2> uvs = new List<Vector2>(vertices.Capacity);

        for (int x = 0; x <= xCount; x++)
        {
            for (int z = 0; z <= zCount; z++)
            {
                // 圆周方向要闭合：最后一列的角度回到 0，和第一列重合
                float angle = x == xCount ? 0f : x * Mathf.PI * 2f / xCount;

                Vector3 baseVertex = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    z * scale * Mathf.PI);

                float u = x == xCount ? 0f : x * scale;
                uvs.Add(new Vector2(u, z * scale));

                // 柏林噪声让每个顶点沿半径方向起伏，形成自然的地形
                float px = baseVertex.x * perlinScale + offset;
                float pz = baseVertex.z * perlinScale + offset;
                Vector3 center = new Vector3(0f, 0f, baseVertex.z);
                Vector3 dir = (center - baseVertex).normalized;
                Vector3 vert = baseVertex + dir * Mathf.PerlinNoise(px, pz) * waveHeight;

                // 前几排朝上一块的末排顶点插值，接缝处就不会裂开；
                // 首块没有上一块数据，跳过即可
                if (z < startTransitionLength && beginPoints != null)
                {
                    float t = z / (float)startTransitionLength;
                    Vector3 begin = new Vector3(beginPoints[x].x, beginPoints[x].y, vert.z);
                    vert = Vector3.Lerp(begin, vert, t);
                }
                else if (z == zCount)
                {
                    // 记下这一列的末排顶点，等下一块地形来接
                    if (beginPoints == null)
                    {
                        beginPoints = new Vector3[xCount + 1];
                    }
                    beginPoints[x] = vert;
                }

                vertices.Add(vert);

                // 每个顶点都有概率放一个障碍或门
                if (Random.Range(0, startObstacleChance) == 0
                    && !(gate == null && obstacles.Length == 0))
                {
                    CreateItem(vert);
                }
            }
        }

        // 把相邻两列之间的四边形拆成两个三角形
        int strip = zCount + 1;
        List<int> triangles = new List<int>(xCount * zCount * 6);
        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                int i0 = x * strip + z;
                int i1 = i0 + 1;
                int i2 = i0 + strip;
                int i3 = i2 + 1;

                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);

                triangles.Add(i1);
                triangles.Add(i3);
                triangles.Add(i2);
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            triangles = triangles.ToArray(),
        };
        mesh.RecalculateNormals();
        return mesh;
    }

    // 在 vert 处放一个门或障碍，朝向赛道中心，让玩家迎面看到
    private void CreateItem(Vector3 vert)
    {
        Vector3 zCenter = new Vector3(0f, 0f, vert.z);

        GameObject prefab;
        if (Random.Range(0, gateChance) == 0)
        {
            prefab = gate;
        }
        else
        {
            prefab = obstacles[Random.Range(0, obstacles.Length)];
        }

        GameObject newItem = Instantiate(prefab);
        newItem.transform.rotation = Quaternion.LookRotation(zCenter - vert, Vector3.up);
        newItem.transform.position = vert;
        newItem.transform.SetParent(currentPiece.transform, false);
    }

    // 给胎痕等动态物体找一个归属地块
    public Transform GetWorldPiece()
    {
        return pieces[0] != null ? pieces[0].transform : null;
    }
}
