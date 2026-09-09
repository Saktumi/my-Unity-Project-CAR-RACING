using UnityEngine;

// 赛道障碍（刺球）：撞上玩家根节点就通知 GameManager 触发结算。
public class Obstacle : MonoBehaviour
{
    private GameManager manager;

    private void Start()
    {
        manager = FindFirstObjectByType<GameManager>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.transform.root.CompareTag("Player") && manager != null)
        {
            manager.GameOver();
        }
    }
}
