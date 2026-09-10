using TMPro;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗失败提示 — 玩家阵营全灭（OnBattleEnd 以 BattleTeam.Enemy 触发）时，
    /// 在屏幕中央显示红色的「战斗失败」。
    ///
    /// 挂载在 Battle1 场景的 UI 上，Inspector 中拖入居中放置的 TextMeshPro 文本。
    /// 之后的过场切场景由 BattleManager.ReturnToOverworld 负责，本组件只负责表现。
    /// </summary>
    public class BattleFailUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _failText;   // 「战斗失败」文本（居中）

        private void OnEnable()
        {
            BattleEventCenter.OnBattleEnd += HandleBattleEnd;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleEnd -= HandleBattleEnd;
        }

        private void Start()
        {
            if (_failText != null)
                _failText.gameObject.SetActive(false);   // 初始隐藏
        }

        private void HandleBattleEnd(BattleTeam winner)
        {
            if (winner != BattleTeam.Enemy) return;   // 只有玩家失败（敌方获胜）才显示

            if (_failText != null)
            {
                _failText.text = "战斗失败";
                _failText.color = Color.red;
                _failText.gameObject.SetActive(true);
            }
        }
    }
}
