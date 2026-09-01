using System.Collections.Generic;

/// <summary>
/// 整张舞蹈谱面（由 JSON 反序列化）。
/// </summary>
[System.Serializable]
public class DanceChart
{
    public float bpm;             // 节拍（当前简单版仅作记录，不参与计算）
    public float offset;          // 谱面偏移（秒）
    public List<NoteData> notes;  // 音符列表（按 time 升序）
}
