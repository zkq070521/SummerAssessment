using BattleSystem;
using UnityEngine;

/// <summary>
/// 战斗 UI 管理器 — 根据当前回合所属队伍显示/隐藏对应 UI。
///
/// 监听 BattleEventCenter.OnTurnChanged：
///   玩家回合 → 显示攻击按钮面板
///   敌人回合 → 隐藏攻击按钮面板
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("UI 面板")]
    [SerializeField] private GameObject _attackUIPanel;   // 包含单攻/群攻按钮的面板（敌人回合隐藏）

    private void Start()
    {
        BattleEventCenter.OnTurnChanged += ChangeUI;
    }

    private void OnDestroy()
    {
        BattleEventCenter.OnTurnChanged -= ChangeUI;
    }

    /// <summary>
    /// 回合切换时显示/隐藏对应 UI
    /// </summary>
    private void ChangeUI(BattleTeam team)
    {
        if (_attackUIPanel == null)
        {
            Debug.LogWarning("[BattleUIManager] _attackUIPanel 未赋值，请在 Inspector 中拖入攻击按钮面板");
            return;
        }

        bool isPlayerTurn = team == BattleTeam.Player;
        _attackUIPanel.SetActive(isPlayerTurn);

        Debug.Log(isPlayerTurn
            ? "[BattleUIManager] 玩家回合 — 显示攻击按钮 UI"
            : "[BattleUIManager] 敌人回合 — 隐藏攻击按钮 UI");
    }
}
