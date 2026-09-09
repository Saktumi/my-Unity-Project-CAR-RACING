using UnityEngine;

// 第三人称跟拍相机：不直接锁死目标，而是每帧朝目标的角度和高度
// 插值靠近一点，数值越大跟得越快，拍出来的画面更平滑。
public class CameraFollow : MonoBehaviour
{
    public Transform camTarget;   // 跟随目标，场景里指向车辆

    public float distance;        // 相机离目标的水平距离
    public float height;          // 相机相对目标高出多少
    public float rotationDamping; // 角度跟随速度
    public float heightDamping;   // 高度跟随速度

    private void LateUpdate()
    {
        if (camTarget == null)
        {
            return;
        }

        // 先算出这一帧期望的角度和高度
        float wantedRotationAngle = camTarget.eulerAngles.y;
        float wantedHeight = camTarget.position.y + height;

        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // 角度用 LerpAngle，会自动处理 0/360 度环绕；高度用普通 Lerp
        currentRotationAngle =
            Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight =
            Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0f, currentRotationAngle, 0f);

        // 先退到目标身后，再把高度放到算好的位置
        Vector3 pos = camTarget.position - currentRotation * Vector3.forward * distance;
        pos = new Vector3(pos.x, currentHeight, pos.z);
        transform.position = pos;

        transform.LookAt(camTarget);
    }
}
