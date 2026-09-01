using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音符对象池 —— 预热 + 动态扩容，复用 NoteView 实例避免频繁 Instantiate/Destroy。
/// </summary>
public class NotePool
{
    private readonly NoteView _prefab;
    private readonly Transform _parent;
    private readonly Queue<NoteView> _pool = new Queue<NoteView>();

    public NotePool(NoteView prefab, Transform parent, int prewarmCount)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < prewarmCount; i++)
            _pool.Enqueue(CreateInstance());
    }

    /// <summary>取出一个实例（已激活，待配置）</summary>
    public NoteView Get()
    {
        NoteView note = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        note.gameObject.SetActive(true);
        return note;
    }

    /// <summary>回收实例（失活入队）</summary>
    public void Release(NoteView note)
    {
        if (note == null) return;
        note.gameObject.SetActive(false);
        _pool.Enqueue(note);
    }

    private NoteView CreateInstance()
    {
        NoteView instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);
        return instance;
    }
}
