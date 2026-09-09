using UnityEngine;
using UnityEngine.InputSystem;

// 玩家车辆：输入读取、转向和漂移痕迹都在这。
// 输入用 InputSystem 生成的 CarAction，读取放在 C# 最顺手；
// 三个生命周期函数各管一摊：Update 画胎痕、FixedUpdate 做物理、
// LateUpdate 负责转向和让视觉轮子跟着物理轮子走。
public class Car : MonoBehaviour
{
    public Rigidbody rb;
    public Transform[] wheelMeshes;     // 视觉轮子模型
    public WheelCollider[] wheelColliders;

    // RotateSpeed / WheelRotateSpeed 的大小写是历史遗留，场景里已按
    // 这个名字序列化，重构时保留原样，避免 Inspector 里的数值失效
    public int RotateSpeed;             // 车身向目标角旋转的速度
    public int rotationAngle;           // 方向打满时的目标转角
    public int WheelRotateSpeed;        // 轮子自转速度，用来表现滚动

    public Transform[] grassEffects;    // 后轮压到草地时激活的粒子
    public Transform[] skidMarkPivots;  // 胎痕生成点
    public GameObject skidMark;         // 胎痕预制体
    public float skidMarkSize;          // 胎痕整体缩放
    public float skidMarkDelay;         // 漂移时每隔多久生成一条胎痕
    public float minRotationDifference; // 转角差超过它才认为车真的在转

    public float grassEffectOffset;     // 判断后轮压草时向下探测的距离

    public Transform back;              // 车尾位置，悬空时从这里往下压
    public float constantBackForce;     // 悬空时施加的向下压力

    public GameObject ragdoll;          // 撞车后替换车辆的散架模型

    private WorldGenerator worldGenerator;

    private CarAction carAction;        // InputSystem 生成的输入封装
    private float inputHorizontal;      // 键盘/摇杆的水平输入
    private bool mouseLeftPressed;      // 是否按着鼠标左键
    private Vector2 mousePosition;      // 鼠标屏幕坐标

    // 转向用“目标角”实现：不直接改角度，而是每帧朝目标角转一点，
    // 松手后目标角归零，车就会自动回正
    private float lastRotation;         // 上一物理帧的车头朝向
    private float targetRotation;       // 当前希望的车头朝向
    private bool skidMarkRoutine;       // 是否处于漂移中
    private float skidTimer;            // 胎痕生成计时器

    private void Start()
    {
        carAction = new CarAction();
        carAction.Enable();

        worldGenerator = FindFirstObjectByType<WorldGenerator>();
    }

    private void Update()
    {
        // 漂移状态下，每隔 skidMarkDelay 秒在两个后轮位置各放一条胎痕
        skidTimer += Time.deltaTime;
        if (skidTimer < skidMarkDelay)
        {
            return;
        }
        skidTimer -= skidMarkDelay;

        if (!skidMarkRoutine)
        {
            return;
        }

        Transform piece = worldGenerator.GetWorldPiece();
        for (int i = 0; i < skidMarkPivots.Length; i++)
        {
            Transform pivot = skidMarkPivots[i];
            GameObject mark = Instantiate(skidMark, pivot.position, pivot.rotation);
            mark.transform.SetParent(piece, true);
            mark.transform.localScale = new Vector3(1f, 1f, 4f) * skidMarkSize;
        }
    }

    private void FixedUpdate()
    {
        bool addForce = true; // 两个后轮都悬空时才需要补向下的力
        bool rotated =
            Mathf.Abs(lastRotation - transform.localEulerAngles.y) > minRotationDifference;

        // 只检查两个后轮：压到草地就扬粒子，并让车进入可漂移状态
        for (int i = 0; i < 2; i++)
        {
            Transform wheelMesh = wheelMeshes[i + 2];
            Transform grass = grassEffects[i];

            if (GroundHit(wheelMesh.position, grassEffectOffset * 1.5f))
            {
                if (!grass.gameObject.activeSelf)
                {
                    grass.gameObject.SetActive(true);
                }

                // 把粒子和胎痕起点挪到后轮贴地的位置
                float effectHeight = wheelMesh.position.y - grassEffectOffset;
                Vector3 targetPos = new Vector3(grass.position.x, effectHeight, wheelMesh.position.z);
                grass.position = targetPos;
                skidMarkPivots[i].position = targetPos;

                addForce = false;
            }
            else if (grass.gameObject.activeSelf)
            {
                grass.gameObject.SetActive(false);
            }
        }

        if (addForce)
        {
            // 悬空时从车尾往下压，让车尽快贴回地面
            rb.AddForceAtPosition(Vector3.down * constantBackForce, back.position);
            skidMarkRoutine = false;
        }
        else
        {
            // 有转向输入且车确实在转，才算漂移；回正或直行时停画胎痕
            if (targetRotation != 0f)
            {
                if (rotated && !skidMarkRoutine)
                {
                    skidMarkRoutine = true;
                }
                else if (!rotated && skidMarkRoutine)
                {
                    skidMarkRoutine = false;
                }
            }
            else
            {
                skidMarkRoutine = false;
            }
        }

        lastRotation = transform.localEulerAngles.y;
    }

    private void LateUpdate()
    {
        // 输入放在 LateUpdate 读，保证拿到的是本帧最新值
        inputHorizontal = carAction.CarMap.Horizontal.ReadValue<float>();
        mouseLeftPressed = carAction.CarMap.MouseLeft.ReadValue<float>() > 0.5f;
        mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        // 视觉轮子跟随 WheelCollider 的位置，再叠加一个自转
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            wheelColliders[i].GetWorldPose(out Vector3 pos, out _);
            Transform mesh = wheelMeshes[i];
            mesh.position = pos;
            mesh.Rotate(Vector3.right * Time.deltaTime * WheelRotateSpeed);
        }

        if (mouseLeftPressed || inputHorizontal != 0f)
        {
            UpdateTargetRotation();
        }
        else if (targetRotation != 0f)
        {
            targetRotation = 0f; // 没有输入就回正
        }

        // 保留车身原本的俯仰/翻滚，只替换朝向，再平滑转过去
        Vector3 euler =
            new Vector3(transform.localEulerAngles.x, targetRotation, transform.localEulerAngles.z);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(euler),
            RotateSpeed * Time.deltaTime);
    }

    // 键盘没有输入时，按住鼠标左键点屏幕左/右半区也能转向
    private void UpdateTargetRotation()
    {
        if (inputHorizontal == 0f)
        {
            targetRotation = mousePosition.x > Screen.width * 0.5f ? rotationAngle : -rotationAngle;
        }
        else
        {
            targetRotation = (int)(rotationAngle * inputHorizontal);
        }
    }

    // 撞车后调用：原地生成散架模型，再把正常车辆藏起来
    public void FallApart()
    {
        Instantiate(ragdoll, transform.position, transform.rotation);
        gameObject.SetActive(false);
    }

    // 从某个点向下打一条短射线，判断后轮是否压到地面
    public bool GroundHit(Vector3 origin, float maxDistance)
    {
        return Physics.Raycast(origin, Vector3.down, maxDistance);
    }

    private void OnDestroy()
    {
        if (carAction != null)
        {
            carAction.Dispose();
        }
    }
}
