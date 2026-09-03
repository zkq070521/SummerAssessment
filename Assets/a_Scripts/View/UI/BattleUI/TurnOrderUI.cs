using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 行动顺序 UI — 屏幕左上角竖排显示接下来行动的 5 个单位头像。
    /// 每个槽位由一个背景板（父 Image）+ 一个头像（子 Image）组成，动效时二者整体移动。
    ///
    /// 最上方为当前行动者（放大 1.3 倍），随回合切换与单位阵亡实时刷新。
    /// 每次刷新时各槽位从下方上滑进入（带错峰），形成「下一位上滑」的动效。
    ///
    /// 挂载在 BattleCanvas 上，将 5 个槽位（背景板 + 头像）从上到下拖入 _orderSlots。
    /// 注意：请手动摆放，不要套 Vertical Layout Group，否则布局会与滑动动画冲突。
    /// </summary>
    public class TurnOrderUI : MonoBehaviour
    {
        [Header("行动顺序槽位")]
        [SerializeField] private TurnOrderSlot[] _orderSlots;   // 从上到下 5 个槽位（背景板 + 头像）

        [Header("滑动动效")]
        [SerializeField] private float _slideDistance = 40f;      // 上滑起始偏移（像素）
        [SerializeField] private float _slideDuration = 0.3f;     // 滑动时长（秒）
        [SerializeField] private float _staggerDelay = 0.06f;     // 相邻槽位错峰延迟（秒）
        [SerializeField] private Ease _slideEase = Ease.OutCubic; // 缓动类型

        [Header("当前行动者")]
        [SerializeField] private float _currentActorScale = 1.3f; // 首位（当前行动者）放大倍率

        // ── 缓存 ──

        /// <summary>每个槽位背景板的静止 anchoredPosition（Awake 时缓存，用于滑动动画）</summary>
        private Vector2[] _restPositions;

        // ── 生命周期 ──

        private void Awake()
        {
            CacheRestPositions();
        }

        private void OnEnable()
        {
            BattleEventCenter.OnBattleStart += HandleBattleStart;
            BattleEventCenter.OnTurnStart += HandleTurnStart;
            BattleEventCenter.OnUnitDeath += HandleUnitDeath;
        }

        private void OnDisable()
        {
            BattleEventCenter.OnBattleStart -= HandleBattleStart;
            BattleEventCenter.OnTurnStart -= HandleTurnStart;
            BattleEventCenter.OnUnitDeath -= HandleUnitDeath;
        }

        private void Start()
        {
            RefreshOrder();
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        // ── 事件回调 ──

        private void HandleBattleStart() => RefreshOrder();
        private void HandleTurnStart(BattleEntityData entity) => RefreshOrder();
        private void HandleUnitDeath(BattleEntityData entity) => RefreshOrder();

        // ── 刷新逻辑 ──

        /// <summary>
        /// 重建行动顺序：当前行动者置于首位，其余按行动值（AV）升序排列，取前 N 个。
        /// 单位阵亡时 OnUnitDeath 先于 TurnManager.RemoveUnit 触发，故必须按 isAlive 过滤。
        /// </summary>
        private void RefreshOrder()
        {
            if (_orderSlots == null || _orderSlots.Length == 0) return;

            BattleManager bm = BattleManager.Instance;
            if (bm == null || bm.TurnManager == null || !bm.IsBattleStarted)
            {
                HideAllSlots();
                return;
            }

            List<BattleEntityData> display = BuildDisplayOrder(bm);

            for (int i = 0; i < _orderSlots.Length; i++)
            {
                TurnOrderSlot slot = _orderSlots[i];
                if (slot == null) continue;

                bool hasUnit = i < display.Count && display[i] != null;

                // 背景板与头像同步显隐
                if (slot.background != null)
                    slot.background.enabled = hasUnit;
                if (slot.icon != null)
                {
                    slot.icon.enabled = hasUnit;
                    slot.icon.sprite = hasUnit ? display[i].icon : null;
                }

                if (hasUnit && slot.background != null)
                    AnimateSlotIn(slot, i);
            }
        }

        /// <summary>组装显示顺序：当前行动者 + 其余按 AV 升序</summary>
        private List<BattleEntityData> BuildDisplayOrder(BattleManager bm)
        {
            List<BattleEntityData> display = new List<BattleEntityData>(_orderSlots.Length + 1);
            if (bm.CurrentActor != null && bm.CurrentActor.isAlive)
                display.Add(bm.CurrentActor);

            IReadOnlyList<BattleEntityData> order = bm.TurnManager.GetTurnOrder();
            for (int i = 0; i < order.Count; i++)
            {
                BattleEntityData unit = order[i];
                if (unit != null && unit != bm.CurrentActor && unit.isAlive)
                    display.Add(unit);
            }
            return display;
        }

        /// <summary>槽位上滑进入 + 首位放大（动效作用于背景板，头像随其移动）</summary>
        private void AnimateSlotIn(TurnOrderSlot slot, int index)
        {
            RectTransform rect = slot.background.rectTransform;
            rect.DOKill();

            // 上滑：从静止位置下方滑入
            Vector2 rest = _restPositions != null && index < _restPositions.Length
                ? _restPositions[index]
                : rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(rest.x, rest.y - _slideDistance);
            rect.DOAnchorPos(rest, _slideDuration)
                .SetDelay(index * _staggerDelay)
                .SetEase(_slideEase);

            // 首位（当前行动者）放大，其余恢复原大小
            float targetScale = index == 0 ? _currentActorScale : 1f;
            rect.DOScale(targetScale, _slideDuration)
                .SetDelay(index * _staggerDelay)
                .SetEase(_slideEase);
        }

        /// <summary>清空所有槽位并复位（战斗未开始 / 已结束时调用）</summary>
        private void HideAllSlots()
        {
            KillAllTweens();

            for (int i = 0; i < _orderSlots.Length; i++)
            {
                TurnOrderSlot slot = _orderSlots[i];
                if (slot == null) continue;

                if (slot.background != null)
                {
                    slot.background.enabled = false;
                    slot.background.rectTransform.localScale = Vector3.one;
                    if (_restPositions != null && i < _restPositions.Length)
                        slot.background.rectTransform.anchoredPosition = _restPositions[i];
                }
                if (slot.icon != null)
                {
                    slot.icon.enabled = false;
                    slot.icon.sprite = null;
                }
            }
        }

        // ── 辅助 ──

        private void CacheRestPositions()
        {
            if (_orderSlots == null || _orderSlots.Length == 0) return;

            _restPositions = new Vector2[_orderSlots.Length];
            for (int i = 0; i < _orderSlots.Length; i++)
            {
                if (_orderSlots[i] != null && _orderSlots[i].background != null)
                    _restPositions[i] = _orderSlots[i].background.rectTransform.anchoredPosition;
            }
        }

        private void KillAllTweens()
        {
            if (_orderSlots == null) return;
            for (int i = 0; i < _orderSlots.Length; i++)
            {
                if (_orderSlots[i] != null && _orderSlots[i].background != null)
                    _orderSlots[i].background.rectTransform.DOKill();
            }
        }
    }
}
