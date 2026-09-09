using UnityEngine;
using UnityEngine.UI;

// 游戏主流程：计时、计分、结算都集中在这里。
// 过门和撞车不是本类直接发现的，而是 Gate / Obstacle 分别调用
// UpdateScore / GameOver 把事件汇报上来，职责更清楚。
public class GameManager : MonoBehaviour
{
    public float time;                // 本局已进行时间（秒），Inspector 里能直接看到
    public Text scoreLabel;           // 游戏中的分数文本
    public Text timeLabel;            // 游戏中的计时文本

    public Car car;                   // 玩家车辆，结算时要让它散架
    public Text gameOverScoreLabel;   // 结算面板上的本局得分
    public Text gameOverBestLabel;    // 结算面板上的历史最高分

    public Animator animator;         // 结算面板的动画控制器

    private int score;
    private bool gameOver;            // 防止结算逻辑被触发两次
    private float secondTimer;        // 每秒自动加分的计时器

    private void Update()
    {
        UpdateTimer();

        // 存活状态下每秒自动加 1 分
        secondTimer += Time.deltaTime;
        if (secondTimer >= 1f)
        {
            secondTimer -= 1f;
            score += 1;
            scoreLabel.text = score.ToString();
        }
    }

    private void UpdateTimer()
    {
        time += Time.deltaTime;

        int total = Mathf.FloorToInt(time);
        int seconds = total % 60;
        int minutes = total / 60;
        timeLabel.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    // 过门加分：由 Gate 组件调用
    public void UpdateScore(int points)
    {
        score += points;
        scoreLabel.text = score.ToString();
    }

    // 撞车结算：由 Obstacle 组件调用
    public void GameOver()
    {
        if (gameOver) return;

        gameOver = true;
        SaveBestScore();

        // 车散架、结算面板淡入
        car.FallApart();
        animator.SetBool("Game over", true);

        // 把场景里所有移动地块停住，赛道不再往前跑
        BasicMovement[] movers = FindObjectsByType<BasicMovement>(FindObjectsSortMode.None);
        foreach (BasicMovement mover in movers)
        {
            mover.movespeed = 0f;
            mover.rotateSpeed = 0f;
        }
    }

    private void SaveBestScore()
    {
        if (score > PlayerPrefs.GetInt("bests"))
        {
            PlayerPrefs.SetInt("bests", score);
        }

        gameOverScoreLabel.text = "score:" + score;
        gameOverBestLabel.text = "best:" + PlayerPrefs.GetInt("bests");
    }
}
