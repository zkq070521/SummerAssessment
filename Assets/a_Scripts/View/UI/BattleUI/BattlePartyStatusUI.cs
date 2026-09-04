using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 战斗队伍血条同步器 — 将 BattleManager.PlayerTeam 的实时血量同步到
    /// TeamPanel 各角色 icon 下方的 HPBar（fillAmount）与 HPText（血量数字）。
    ///
    /// 与 TurnOrderUI / DamageNumberSpawner 同属表现层，通过 BattleEventCenter 事件
    /// 与逻辑层解耦：逻辑层只广播 OnBattleStart / OnDamageDealt / OnUnitDeath，
    /// 本组件据此刷新血条，不直接读写角色模型或战斗数据。
    ///
    /// 挂载在 Battle1 场景的 TeamPanel 上。在 Inspector 中按 BattleManager.PlayerTeam
    /// 的顺序拖入各角色槽位的 HPBar 的 Fill 与 HPText（见 BattlePartySlot）。
    /// </summary>
    public class BattlePartyStatusUI : MonoBehaviour
    {
        [Header("队伍槽位（按 PlayerTeam 顺序拖入）")]
        [SerializeField] private BattlePartySlot[] _slots;

        // ── 生命周期 ──

        private void OnEnable()
        {
            BattleEventCenter.OnBattleStart += HandleBattleStart;
            BattleEventCenter.OnDamageDealt += HandleDamageDealt;
            BattleEventCenter.OnUnitDeath += HandleUnitDeath;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleStart -= HandleBattleStart;
            BattleEventCenter.OnDamageDealt -= HandleDamageDealt;
            BattleEventCenter.OnUnitDeath -= HandleUnitDeath;
        }

        private void Start()
        {
            if (_slots == null || _slots.Length == 0)
            {
                Debug.LogWarning("[BattlePartyStatusUI] _slots 未赋值，请在 Inspector 中按 PlayerTeam 顺序拖入槽位");
                return;
            }

            RefreshHpBars();
        }

        // ── 事件回调 ──

        private void HandleBattleStart() => RefreshHpBars();

        private void HandleDamageDealt(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
            => RefreshHpBars();

        private void HandleUnitDeath(BattleEntityData entity) => RefreshHpBars();

        // ── 刷新逻辑 ──

        /// <summary>
        /// 从 BattleManager.PlayerTeam 按索引读取血量，写入各槽位 HPBar 的 fillAmount 与 HPText 文本。
        /// OnDamageDealt / OnUnitDeath 均在 RemoveFromTeam 之前广播（见 BattleManager.ExecuteAction），
        /// 因此回调触发时实体仍在列表中、currentHP 已更新，索引映射安全。
        /// </summary>
        private void RefreshHpBars()
        {
            if (_slots == null || _slots.Length == 0)
                return;

            BattleManager battle = BattleManager.Instance;
            if (battle == null || !battle.IsBattleStarted)
                return; // 战斗未开始，PlayerTeam 尚未创建，保留 TeamUI 的静态满血显示

            IReadOnlyList<BattleEntityData> team = battle.PlayerTeam;

            for (int i = 0; i < _slots.Length; i++)
            {
                BattlePartySlot slot = _slots[i];
                if (slot == null) continue;

                bool hasEntity = i < team.Count && team[i] != null;
                float ratio = hasEntity && team[i].maxHP > 0f
                    ? team[i].currentHP / team[i].maxHP
                    : 0f;

                if (slot.hpBarFill != null)
                    slot.hpBarFill.fillAmount = ratio;

                if (slot.hpText != null)
                    slot.hpText.text = hasEntity ? Mathf.RoundToInt(team[i].currentHP).ToString() : string.Empty;
            }
        }
    }
}
