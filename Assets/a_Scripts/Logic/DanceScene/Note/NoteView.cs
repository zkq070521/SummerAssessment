using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个音符视图 —— 持有一个 Image 的 UI 音符，颜色随左右轨道区分。
/// 位置由 NoteSpawner 每帧根据时间驱动，本类不自行移动。
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(Image))]
public class NoteView : MonoBehaviour
{
    /// <summary>音符类型（左/右）</summary>
    public NoteType Type { get; private set; }

    /// <summary>音符到达判准线的时刻（秒）</summary>
    public float HitTime { get; private set; }

    /// <summary>RectTransform（供生成器定位）</summary>
    public RectTransform Rect { get; private set; }

    private Image _image;

    private static readonly Color LeftColor = new Color(0.30f, 0.70f, 1.00f, 1f);  // 蓝
    private static readonly Color RightColor = new Color(1.00f, 0.50f, 0.30f, 1f); // 橙

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    /// <summary>由生成器在取出时配置音符类型与到达时刻</summary>
    public void Initialize(NoteType type, float hitTime)
    {
        Type = type;
        HitTime = hitTime;

        if (_image != null)
            _image.color = type == NoteType.Left ? LeftColor : RightColor;
    }
}
