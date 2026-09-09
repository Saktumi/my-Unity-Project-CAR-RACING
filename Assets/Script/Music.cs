using UnityEngine;

// 跨场景背景音乐：第一个实例保留下来并进入下一场景，
// 之后新场景里出现的重复实例直接销毁，保证音乐不会叠加。
public class Music : MonoBehaviour
{
    private static int musicId;

    private void Awake()
    {
        int id = GetInstanceID();
        if (musicId != 0 && musicId != id)
        {
            Destroy(gameObject);
            return;
        }

        musicId = id;
        DontDestroyOnLoad(gameObject);
    }
}
