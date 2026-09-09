using UnityEngine;

// 调试用的噪声演示：场景里的两条 Line 分别用柏林噪声和纯随机画一条线，
// 用来直观对比两者差异。勾选 UsePerLin 走柏林噪声。
public class TestPerlin : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private float _a = 0.06f;
    public bool UsePerLin;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        Vector3[] positions = new Vector3[100];
        for (int i = 0; i < positions.Length; i++)
        {
            if (UsePerLin)
            {
                // 相邻采样点变化连续，看起来像平滑起伏的山丘
                positions[i] = new Vector3(i * 0.1f, Mathf.PerlinNoise(i * _a, i * _a), 0f);
            }
            else
            {
                // 纯随机：每个点的高度都毫无关联地乱跳
                positions[i] = new Vector3(i * 0.1f, Random.value, 0f);
            }
        }

        _lineRenderer.SetPositions(positions);
    }
}
