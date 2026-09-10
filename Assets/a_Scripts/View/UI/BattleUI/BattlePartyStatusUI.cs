using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// 战斗队伍血条同步器 — 将 BattleManager 双方队伍的实时血量同步到 UI：
    ///   1. PlayerTeam → TeamPanel 各角色 icon 下方的 HPBar（fillAmount）与 HPText（血量数字），受击后立即刷新；
    ///   2. EnemyTeam → 敌人总血条（所有存活敌人 HP 之和 / 开战时总血量上限），受击后延迟 ENEMY_HP_BAR_UPDATE_DELAY 秒再刷新。
    ///
    /// 与 TurnOrderUI / DamageNumberSpawner 同属表现层，通过 BattleEventCenter 事件
    /// 与逻辑层解耦：逻辑层只广播 OnBattleStart / OnDamageDealt / OnUnitDeath，
    /// 本组件据此刷新血条，不直接读写角色模型或战斗数据。
    ///
    /// 挂载在 Battle1 场景的 TeamPanel 上。在 Inspector 中按 BattleManager.PlayerTeam
    /// 的顺序拖入各角色槽位的 HPBar 的 Fill 与 HPText（见 BattlePartySlot），
    /// 并拖入敌人总血条的 Fill 子物体到 _enemyHpBarFill。
    /// </summary>
    public class BattlePartyStatusUI : MonoBehaviour
    {
        /// <summary>敌人总血条受击后的延迟刷新时长（秒），用于等待受击/死亡表现播放后再扣减血条</summary>
        private const float ENEMY_HP_BAR_UPDATE_DELAY = 2f;

        [Header("队伍槽位（按 PlayerTeam 顺序拖入）")]
        [SerializeField] private BattlePartySlot[] _slots;

        [Header("敌人总血条")]
        [SerializeField] private Image _enemyHpBarFill;   // 敌人总血条的 Fill 子物体（Filled 型 Image）

        /// <summary>开战时所有敌人 maxHP 之和（缓存分母：敌人阵亡被移出列表后仍保持不变，避免血条跳满）</summary>
        private float _enemyTotalMaxHp;

        /// <summary>延迟刷新敌人总血条的协程句柄，用于在重复受击时取消旧的、重开计时</summary>
        private Coroutine _enemyHpBarCoroutine;

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

        private void HandleBattleStart()
        {
            CacheEnemyTotalMaxHp();
            RefreshPlayerHpBars();
            ApplyEnemyHpBar();   // 开战时敌人总血条立即显示满血，不延迟
        }

        private void HandleDamageDealt(BattleEntityData source, BattleEntityData target, int damage, bool isCritical)
            => RefreshHpBars();

        private void HandleUnitDeath(BattleEntityData entity) => RefreshHpBars();

        // ── 刷新逻辑 ──

        /// <summary>
        /// 受击/死亡后刷新：玩家血条立即刷新，敌人总血条延迟 ENEMY_HP_BAR_UPDATE_DELAY 秒刷新。
        /// OnDamageDealt / OnUnitDeath 均在 RemoveFromTeam 之前广播（见 BattleManager.ExecuteAction），
        /// 因此回调触发时实体仍在列表中、currentHP 已更新，索引映射安全。
        /// </summary>
        private void RefreshHpBars()
        {
            RefreshPlayerHpBars();
            ScheduleEnemyHpBarUpdate();
        }

        /// <summary>
        /// 从 BattleManager.PlayerTeam 按索引读取血量，写入各槽位 HPBar 的 fillAmount 与 HPText 文本。
        /// </summary>
        private void RefreshPlayerHpBars()
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

        /// <summary>
        /// 调度敌人总血条的延迟刷新：若已有协程在等待，则取消重开，确保血条在最后一次受击后
        /// 再等满 ENEMY_HP_BAR_UPDATE_DELAY 秒才扣减。
        /// </summary>
        private void ScheduleEnemyHpBarUpdate()
        {
            if (_enemyHpBarFill == null)
                return;

            BattleManager battle = BattleManager.Instance;
            if (battle == null || !battle.IsBattleStarted)
                return;

            if (_enemyHpBarCoroutine != null)
            {
                StopCoroutine(_enemyHpBarCoroutine);
                _enemyHpBarCoroutine = null;
            }

            _enemyHpBarCoroutine = StartCoroutine(UpdateEnemyHpBarAfterDelay());
        }

        /// <summary>等待 ENEMY_HP_BAR_UPDATE_DELAY 秒后写入敌人总血条，并清空协程句柄</summary>
        private IEnumerator UpdateEnemyHpBarAfterDelay()
        {
            yield return new WaitForSeconds(ENEMY_HP_BAR_UPDATE_DELAY);
            ApplyEnemyHpBar();
            _enemyHpBarCoroutine = null;
        }

        /// <summary>
        /// 写入敌人总血条：fillAmount = 所有存活敌人 currentHP 之和 / 开战时总血量上限。
        /// 总血量归零时敌人已全部阵亡，逻辑层（BattleManager.CheckBattleEnd）已判定玩家胜利，
        /// 本方法只负责表现，不直接驱动胜负。
        /// </summary>
        private void ApplyEnemyHpBar()
        {
            if (_enemyHpBarFill == null)
                return;

            BattleManager battle = BattleManager.Instance;
            if (battle == null || !battle.IsBattleStarted)
                return;

            IReadOnlyList<BattleEntityData> enemies = battle.EnemyTeam;

            float totalCurrent = 0f;
            foreach (BattleEntityData enemy in enemies)
            {
                if (enemy == null) continue;
                totalCurrent += enemy.currentHP;
            }

            _enemyHpBarFill.fillAmount = _enemyTotalMaxHp > 0f
                ? Mathf.Clamp01(totalCurrent / _enemyTotalMaxHp)
                : 0f;
        }

        /// <summary>
        /// 缓存开战时敌人总血量上限（所有敌人 maxHP 之和）。
        /// 敌人阵亡后会被 RemoveFromTeam 移出 EnemyTeam，若每次用存活敌人重新求和，
        /// 分母会随敌人减少而缩小、导致血条错误地跳满，故缓存一次并保持不变。
        /// </summary>
        private void CacheEnemyTotalMaxHp()
        {
            _enemyTotalMaxHp = 0f;

            BattleManager battle = BattleManager.Instance;
            if (battle == null) return;

            IReadOnlyList<BattleEntityData> enemies = battle.EnemyTeam;
            foreach (BattleEntityData enemy in enemies)
            {
                if (enemy == null) continue;
                _enemyTotalMaxHp += enemy.maxHP;
            }
        }
    }
}
