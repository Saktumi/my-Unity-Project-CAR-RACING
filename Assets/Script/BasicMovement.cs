using UnityEngine;

// 挂在赛道地块（以及场景里个别装饰物）上的移动组件：
// 地块会朝玩家方向匀速前进，看起来像“赛道自己在跑”；
// 车辆转弯时再按比例旋转地块，让道路跟着自然拐弯。
public class BasicMovement : MonoBehaviour
{
    public float movespeed;             // 前进速度
    public float rotateSpeed = 30f;     // 配合车辆转弯时的旋转速度
    public bool lamp;                   // 路灯和地块的旋转轴不同，用这个区分

    private Car car;
    private WorldGenerator generator;

    private void Start()
    {
        car = FindFirstObjectByType<Car>();
        generator = FindFirstObjectByType<WorldGenerator>();
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * movespeed * Time.deltaTime);

        // 主菜单背景没有车，不需要跟着转向
        if (car != null)
        {
            CheckRotation();
        }
    }

    // 车转了多少，地块就反向转多少，让玩家感觉赛道真的弯过来了
    private void CheckRotation()
    {
        Vector3 direction = lamp ? Vector3.right : Vector3.forward;

        float carRotation = car.transform.localEulerAngles.y;
        if (carRotation > car.rotationAngle * 2)
        {
            // 超过半圈说明车往反方向转了，先把角度折算成带符号的数值
            carRotation = (360f - carRotation) * -1f;
        }

        float rotateAmount =
            -rotateSpeed
            * (carRotation / car.rotationAngle)
            * (36f / generator.dimensions.x)
            * Time.deltaTime;

        transform.Rotate(direction * rotateAmount);
    }
}
