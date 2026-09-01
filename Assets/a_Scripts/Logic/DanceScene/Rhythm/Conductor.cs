using UnityEngine;

/// <summary>
/// 舞蹈时间轴 —— 为音符生成与判定提供统一的「歌曲当前位置」。
/// 当前以 Time.time 累计计时（工程暂无 BGM），后续可在此切换到 AudioSettings.dspTime 音频同步。
/// </summary>
public class Conductor : MonoBehaviour
{
    /// <summary>歌曲当前位置（秒，相对开始）</summary>
    public float CurrentTime { get; private set; }

    /// <summary>是否正在播放</summary>
    public bool IsPlaying { get; private set; }

    private float _startTime;

    /// <summary>开始计时</summary>
    public void Begin()
    {
        _startTime = Time.time;
        CurrentTime = 0f;
        IsPlaying = true;
    }

    /// <summary>停止计时</summary>
    public void Stop()
    {
        IsPlaying = false;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        CurrentTime = Time.time - _startTime;
    }
}
