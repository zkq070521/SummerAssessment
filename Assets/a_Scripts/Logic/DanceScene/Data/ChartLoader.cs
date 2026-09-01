using UnityEngine;

/// <summary>
/// 谱面加载器 —— 从 Resources 读取 JSON 文本并反序列化为 DanceChart。
/// </summary>
public static class ChartLoader
{
    /// <summary>
    /// 从 Resources/Charts 下加载谱面（路径不含扩展名，如 "Charts/Song1"）。
    /// </summary>
    public static DanceChart Load(string resourcePath)
    {
        TextAsset text = Resources.Load<TextAsset>(resourcePath);
        if (text == null)
        {
            Debug.LogError($"[ChartLoader] 未找到谱面资源：Resources/{resourcePath}");
            return null;
        }

        DanceChart chart = JsonUtility.FromJson<DanceChart>(text.text);
        if (chart == null || chart.notes == null)
        {
            Debug.LogError($"[ChartLoader] 谱面解析失败：{resourcePath}");
            return null;
        }

        // 按时间升序排序，保证生成顺序稳定
        chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
        return chart;
    }
}
