using System.Collections;
using UnityEngine;

/// <summary>
/// 战斗结束状态 — 判定胜负、结算奖励、返回主世界
/// </summary>
public class BattleEndState : IBattleState
{
    private readonly BattleStateManager _manager;
    private Coroutine _routine;

    // 结算事件
    public System.Action<BattleResult> OnBattleEnd;

    public BattleEndState(BattleStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        BattleResult result = _manager.CheckBattleEnd();

        if (_manager.battleUI != null)
            _manager.battleUI.ShowActionPanel(false);

        _routine = _manager.StartCoroutine(EndBattleRoutine(result));
    }

    public void Execute() { }

    public void Exit()
    {
        if (_routine != null)
        {
            _manager.StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator EndBattleRoutine(BattleResult result)
    {
        // 1. 播报结果
        string resultText = result switch
        {
            BattleResult.Victory => "战斗胜利！",
            BattleResult.Defeat => "战斗失败...",
            BattleResult.Flee => "成功逃跑",
            _ => "战斗结束"
        };

        if (_manager.battleUI != null)
            _manager.battleUI.SetStatusText(resultText);

        // 2. 胜利结算
        if (result == BattleResult.Victory)
        {
            int expReward = CalculateExpReward();
            int goldReward = CalculateGoldReward();

            if (_manager.battleUI != null)
                _manager.battleUI.AddBattleLog($"获得 {expReward} 经验值，{goldReward} 金币");

            // 通知 GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.AddBattleReward(expReward, goldReward);
        }

        // 3. 触发战斗结束事件（摄像机响应）
        BattleEventCenter.TriggerBattleEnd(result);

        // 4. 等待播放时间
        yield return new WaitForSeconds(1.5f);

        // 5. 触发结算回调
        OnBattleEnd?.Invoke(result);

        // 6. 返回主世界
        //SceneTransitionManager.Instance.LoadScene(_manager.overworldSceneName);
    }

    private int CalculateExpReward()
    {
        int total = 0;
        foreach (var enemy in _manager.EnemyParty)
        {
            total += Mathf.RoundToInt(enemy.attack * 5f + enemy.maxHP * 0.5f);
        }
        return total;
    }

    private int CalculateGoldReward()
    {
        int total = 0;
        foreach (var enemy in _manager.EnemyParty)
        {
            total += Mathf.RoundToInt(enemy.maxHP * 0.3f);
        }
        return Mathf.Max(1, total);
    }
}
