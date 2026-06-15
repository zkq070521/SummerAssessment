using System.Collections;
using UnityEngine;

/// <summary>
/// 战斗开始状态 — 加载场景、初始化实体、播放入场过渡
/// </summary>
public class BattleBeginState : IBattleState
{
    private readonly BattleStateManager _manager;
    private Coroutine _routine;

    public BattleBeginState(BattleStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        if (_manager == null)
        {
            Debug.LogError("[BattleBeginState] BattleStateManager 引用为空");
            return;
        }

        _routine = _manager.StartCoroutine(BeginBattleRoutine());
    }

    public void Execute()
    {
        // 入场阶段无每帧逻辑
    }

    public void Exit()
    {
        if (_routine != null)
        {
            _manager.StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator BeginBattleRoutine()
    {
        var transition = SceneTransitionManager.Instance;

        // 1. 过渡进入战斗场景
        transition.LoadScene(_manager.battleSceneName);
        // 等待过渡完成
        yield return new WaitUntil(() => !transition.IsTransitioning);

        // 2. 初始化 UI
        if (_manager.battleUI != null)
            _manager.battleUI.Initialize(_manager);

        // 3. 播放入场效果（淡入、文字提示）
        if (_manager.battleUI != null)
            yield return _manager.StartCoroutine(_manager.battleUI.ShowBattleStartEffect());

        // 4. 切换到玩家回合
        _manager.AdvanceTurn();
        _manager.ChangeState<PlayerTurnState>();
    }
}
