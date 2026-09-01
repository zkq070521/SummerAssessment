/// <summary>
/// 单个音符的谱面数据（由 JSON 反序列化）。
/// </summary>
[System.Serializable]
public class NoteData
{
    public float time;     // 音符到达判准线的时刻（秒，相对歌曲开始）
    public NoteType type;  // 左 / 右
}
