using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音符生成器 —— 按谱面时间调度音符生成，持有对象池，并驱动活动音符沿轨道下落。
/// </summary>
public class NoteSpawner : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private NoteView _notePrefab;
    [SerializeField] private Conductor _conductor;

    [Header("轨道几何（锚点坐标）")]
    [SerializeField] private float _fallDuration = 2f;   // 音符从顶部落到判准线的时长（秒）
    [SerializeField] private float _spawnY = 600f;       // 顶部生成 Y
    [SerializeField] private float _lineY = -400f;       // 判准线 Y
    [SerializeField] private float _laneOffsetX = 200f;  // 左右轨道相对中心的 X 偏移

    [Header("对象池")]
    [SerializeField] private int _prewarmCount = 20;     // 预热数量

    private NotePool _pool;
    private DanceChart _chart;
    private int _nextNoteIndex;
    private readonly List<NoteView> _activeNotes = new List<NoteView>();

    /// <summary>当前活动（已生成、未命中/未漏接）的音符列表</summary>
    public IReadOnlyList<NoteView> ActiveNotes => _activeNotes;

    /// <summary>载入谱面并重建对象池</summary>
    public void LoadChart(DanceChart chart)
    {
        _chart = chart;
        _nextNoteIndex = 0;
        _pool = new NotePool(_notePrefab, transform, _prewarmCount);
    }

    private void Update()
    {
        if (_chart == null || _conductor == null || !_conductor.IsPlaying) return;

        float t = _conductor.CurrentTime;
        SpawnDueNotes(t);
        UpdateActiveNotes(t);
    }

    /// <summary>生成所有已到生成时刻的音符</summary>
    private void SpawnDueNotes(float currentTime)
    {
        while (_nextNoteIndex < _chart.notes.Count)
        {
            NoteData data = _chart.notes[_nextNoteIndex];
            if (data.time - _fallDuration > currentTime) break;

            NoteView note = _pool.Get();
            note.Initialize(data.type, data.time);
            note.Rect.anchoredPosition = new Vector2(LaneX(data.type), _spawnY);
            _activeNotes.Add(note);
            _nextNoteIndex++;
        }
    }

    /// <summary>按时间反推每个活动音符的 Y 位置（1=顶部，0=判准线）</summary>
    private void UpdateActiveNotes(float currentTime)
    {
        foreach (NoteView note in _activeNotes)
        {
            float progress = Mathf.Clamp01((note.HitTime - currentTime) / _fallDuration);
            note.Rect.anchoredPosition = new Vector2(note.Rect.anchoredPosition.x, Mathf.Lerp(_lineY, _spawnY, progress));
        }
    }

    /// <summary>回收一个音符到对象池（命中或漏接后调用）</summary>
    public void ReleaseNote(NoteView note)
    {
        _activeNotes.Remove(note);
        _pool.Release(note);
    }

    private float LaneX(NoteType type) => (type == NoteType.Left ? -1f : 1f) * _laneOffsetX;
}
