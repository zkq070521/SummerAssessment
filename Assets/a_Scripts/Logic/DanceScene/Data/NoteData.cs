/// <summary>
/// 单个音符的谱面数据（由 JSON 反序列化）。
/// 注意：Unity JsonUtility 把枚举序列化为整数，无法从 "Left"/"Right" 字符串反序列化，
/// 因此 type 用字符串存储，再通过 Type 属性映射为 NoteType。
/// </summary>
[System.Serializable]
public class NoteData
{
    public float time;      // 音符到达判准线的时刻（秒，相对歌曲开始）
    public string type;     // "Left" / "Right"

    /// <summary>解析后的音符类型（无法识别时默认 Left）</summary>
    public NoteType Type => type == "Right" ? NoteType.Right : NoteType.Left;
}
