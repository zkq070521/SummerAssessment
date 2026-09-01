using UnityEngine;

/// <summary>
/// 判定管理器 —— 读取 A/D 输入，对当前活动音符做命中判定，并处理越过判准线后的漏接。
/// </summary>
public class JudgeManager : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private Conductor _conductor;
    [SerializeField] private NoteSpawner _noteSpawner;

    [Header("判定窗口（秒）")]
    [SerializeField] private float _perfectWindow = 0.15f;
    [SerializeField] private float _goodWindow = 0.5f;

    /// <summary>判定结果事件：参数为音符类型与判定结果</summary>
    public event System.Action<NoteType, JudgeResult> OnJudged;

    private void Update()
    {
        if (_conductor == null || !_conductor.IsPlaying) return;

        if (Input.GetKeyDown(KeyCode.A)) Judge(NoteType.Left);
        if (Input.GetKeyDown(KeyCode.D)) Judge(NoteType.Right);

        AutoMissCheck();
    }

    /// <summary>对指定轨道做命中判定（窗口内找不到音符则忽略此次按键）</summary>
    private void Judge(NoteType lane)
    {
        NoteView note = FindBestMatch(lane);
        if (note == null) return;

        float delta = Mathf.Abs(note.HitTime - _conductor.CurrentTime);
        JudgeResult result = delta <= _perfectWindow ? JudgeResult.Perfect : JudgeResult.Good;

        _noteSpawner.ReleaseNote(note);
        OnJudged?.Invoke(lane, result);
    }

    /// <summary>扫描越过判准线且超时未命中的音符 → 记为 Miss 并回收</summary>
    private void AutoMissCheck()
    {
        for (int i = _noteSpawner.ActiveNotes.Count - 1; i >= 0; i--)
        {
            NoteView note = _noteSpawner.ActiveNotes[i];
            if (_conductor.CurrentTime - note.HitTime > _goodWindow)
            {
                _noteSpawner.ReleaseNote(note);
                OnJudged?.Invoke(note.Type, JudgeResult.Miss);
            }
        }
    }

    /// <summary>在指定轨道内，找距当前时间最近且落在窗口内的活动音符</summary>
    private NoteView FindBestMatch(NoteType lane)
    {
        NoteView best = null;
        float bestDelta = float.MaxValue;

        foreach (NoteView note in _noteSpawner.ActiveNotes)
        {
            if (note.Type != lane) continue;

            float delta = Mathf.Abs(note.HitTime - _conductor.CurrentTime);
            if (delta <= _goodWindow && delta < bestDelta)
            {
                best = note;
                bestDelta = delta;
            }
        }

        return best;
    }
}
