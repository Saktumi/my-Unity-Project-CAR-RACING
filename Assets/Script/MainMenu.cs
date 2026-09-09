using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 主菜单逻辑：按 Enter（或点开始按钮）后等待 0.6 秒再切进游戏场景，
// 给菜单到游戏之间留一点过渡时间。
public class MainMenu : MonoBehaviour
{
    private bool loading;    // 已经开始切场景，防止连点触发多次加载
    private float remaining; // 进入游戏前的剩余等待时间

    private void Update()
    {
        // 键盘 Enter 直接开始；UI 按钮也可以通过 StartGame 触发
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            StartGame();
        }

        if (!loading) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            loading = false;
            SceneManager.LoadScene("Game");
        }
    }

    public void StartGame()
    {
        if (loading) return;

        loading = true;
        remaining = 0.6f;
    }
}
