/// <summary>
/// 战斗状态接口 — 所有战斗状态均实现此接口
/// </summary>
public interface IBattleState
{
    /// <summary>进入状态时调用（初始化、播放过渡）</summary>
    void Enter();

    /// <summary>每帧执行（状态逻辑、检查条件）</summary>
    void Execute();

    /// <summary>退出状态时调用（清理）</summary>
    void Exit();
}
