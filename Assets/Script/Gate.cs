using UnityEngine;

// 赛道上的金色门框：玩家第一次穿过时通知 GameManager 加 100 分。
// 用 addedScore 保证同一个门只结算一次，防止在门口来回蹭分。
public class Gate : MonoBehaviour
{
    private GameManager manager;
    private bool addedScore;

    private void Start()
    {
        manager = FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (addedScore) return;

        // 只有玩家（根节点带 Player 标签）穿过才算数
        if (!other.transform.root.CompareTag("Player")) return;

        addedScore = true;
        if (manager != null)
        {
            manager.UpdateScore(100);
        }
    }
}
