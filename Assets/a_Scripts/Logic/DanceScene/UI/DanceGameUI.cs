using TMPro;
using UnityEngine;

/// <summary>
/// 音游 UI —— 显示分数、连击与最近一次判定结果文字。
/// 分数/连击由 ScoreManager 驱动，判定文字由 JudgeManager 驱动。
/// </summary>
public class DanceGameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _comboText;
    [SerializeField] private TextMeshProUGUI _judgeText;

    /// <summary>更新分数与连击显示</summary>
    public void OnScoreChanged(int score, int combo, int maxCombo)
    {
        if (_scoreText != null) _scoreText.text = $"分数 {score}";
        if (_comboText != null) _comboText.text = combo > 0 ? $"连击 {combo}" : string.Empty;
    }

    /// <summary>显示最近一次判定结果</summary>
    public void OnJudged(JudgeResult result)
    {
        if (_judgeText != null) _judgeText.text = ResultLabel(result);
    }

    private static string ResultLabel(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect: return "完美";
            case JudgeResult.Good: return "不错";
            case JudgeResult.Miss: return "漏接";
            default: return string.Empty;
        }
    }
}
