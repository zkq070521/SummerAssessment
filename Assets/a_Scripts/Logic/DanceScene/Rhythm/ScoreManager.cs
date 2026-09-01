using UnityEngine;

/// <summary>
/// 计分管理器 —— 维护分数、连击、最大连击，并根据判定结果更新。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("分值")]
    [SerializeField] private int _perfectScore = 300;
    [SerializeField] private int _goodScore = 150;

    /// <summary>当前总分</summary>
    public int Score { get; private set; }

    /// <summary>当前连击数</summary>
    public int Combo { get; private set; }

    /// <summary>历史最大连击数</summary>
    public int MaxCombo { get; private set; }

    /// <summary>分数变化事件：参数为（总分，连击，最大连击）</summary>
    public event System.Action<int, int, int> OnScoreChanged;

    /// <summary>根据一次判定结果更新分数与连击</summary>
    public void RegisterJudge(JudgeResult result)
    {
        switch (result)
        {
            case JudgeResult.Perfect:
                Score += _perfectScore;
                Combo++;
                MaxCombo = Mathf.Max(MaxCombo, Combo);
                break;

            case JudgeResult.Good:
                Score += _goodScore;
                Combo++;
                MaxCombo = Mathf.Max(MaxCombo, Combo);
                break;

            case JudgeResult.Miss:
                Combo = 0;
                break;
        }

        OnScoreChanged?.Invoke(Score, Combo, MaxCombo);
    }

    /// <summary>重置所有计分状态</summary>
    public void ResetScore()
    {
        Score = 0;
        Combo = 0;
        MaxCombo = 0;
        OnScoreChanged?.Invoke(Score, Combo, MaxCombo);
    }
}
