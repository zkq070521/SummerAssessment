using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包面板控制器 — 背包 UI 的开关、拖拽、装备、悬停提示（纯表现层，不持有数据）。
///
/// 布局：面板左侧 5 个装备槽（头 + 四肢），右侧 4×4 共 16 个背包格子。
/// 所有物品/装备数据由持久单例 InventoryManager 保存，本组件通过 InventoryManager.Instance 读写。
/// 交互：
///   1. BagButton 点击 → Open()；右上角退出按钮 → Close()；
///   2. 拖拽背包物品到另一格 = 交换位置；拖到装备槽 = 装备（需部位匹配）；
///   3. 悬停物品 = 显示名称 + 简介。
/// </summary>
public class BagPanelController : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject _bagPanel;          // 背包面板根物体
    [SerializeField] private Button _exitButton;            // 右上角退出按钮

    [Header("槽位（可留空，Awake 按层级顺序自动收集）")]
    [SerializeField] private EquipmentSlot[] _equipmentSlots;   // 5 个装备槽
    [SerializeField] private BagSlot[] _bagSlots;               // 16 个背包格子

    [Header("拖拽")]
    [SerializeField] private Image _dragIcon;               // 跟随鼠标的拖拽图标（Canvas 直接子物体）

    [Header("悬停提示")]
    [SerializeField] private RectTransform _tooltipRect;    // 提示框（Canvas 直接子物体）
    [SerializeField] private TMP_Text _tooltipName;         // 物品名称文本
    [SerializeField] private TMP_Text _tooltipDesc;         // 物品简介文本
    [SerializeField] private Vector2 _tooltipOffset = new Vector2(400f, -80f);   // 提示框相对鼠标的偏移

    /// <summary>正在拖拽的背包格子索引，-1 表示未拖拽</summary>
    private int _dragSourceIndex = -1;

    private Canvas _canvas;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.GetComponent<RectTransform>();

        // 拖拽图标与提示框不能遮挡指针检测（否则拖拽命中 / 悬停会失效）
        if (_dragIcon != null)
            _dragIcon.raycastTarget = false;
        if (_tooltipRect != null)
            foreach (Graphic g in _tooltipRect.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

        // 未手动拖入槽位时，按层级顺序自动收集子物体上的槽位组件（顺序即索引）
        if (_bagSlots == null || _bagSlots.Length == 0)
            _bagSlots = GetComponentsInChildren<BagSlot>(true);
        if (_equipmentSlots == null || _equipmentSlots.Length == 0)
            _equipmentSlots = GetComponentsInChildren<EquipmentSlot>(true);

        if (_bagSlots.Length != InventoryManager.BAG_SLOT_COUNT)
            Debug.LogWarning($"[BagPanelController] _bagSlots 应配置 {InventoryManager.BAG_SLOT_COUNT} 个格子，实际 {_bagSlots.Length} 个");
        if (_equipmentSlots.Length != InventoryManager.EQUIP_SLOT_COUNT)
            Debug.LogWarning($"[BagPanelController] _equipmentSlots 应配置 {InventoryManager.EQUIP_SLOT_COUNT} 个槽位，实际 {_equipmentSlots.Length} 个");

        for (int i = 0; i < _bagSlots.Length; i++)
            _bagSlots[i]?.Initialize(i, this);
        for (int i = 0; i < _equipmentSlots.Length; i++)
            _equipmentSlots[i]?.Initialize(i, this);

        if (_exitButton != null)
            _exitButton.onClick.AddListener(Close);
    }

    private void Start()
    {
        Close();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(Close);
    }

    // ── 开关 ──

    public void Open()
    {
        if (_bagPanel != null)
            _bagPanel.SetActive(true);

        ShowCursor(true);   // 打开背包显示鼠标

        RefreshUI();
    }

    public void Close()
    {
        if (_bagPanel != null)
            _bagPanel.SetActive(false);

        ShowCursor(false);  // 关闭背包隐藏鼠标（锁定），与对话系统一致

        HideTooltip();
        HideDragIcon();
        _dragSourceIndex = -1;
    }

    /// <summary>显示/隐藏鼠标并切换锁定模式（与 DialogueController.ShowCursor 保持一致）</summary>
    private void ShowCursor(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ── 拖拽（由 BagSlot 转发调用）──

    public void BeginDrag(int bagIndex, Vector2 screenPosition)
    {
        ItemData item = GetItem(bagIndex);
        if (item == null) return;

        _dragSourceIndex = bagIndex;
        if (_dragIcon != null)
        {
            _dragIcon.sprite = item.icon;
            _dragIcon.gameObject.SetActive(true);
            MoveDragIcon(screenPosition);
        }
    }

    public void Drag(Vector2 screenPosition) => MoveDragIcon(screenPosition);

    public void EndDrag(PointerEventData eventData)
    {
        HideDragIcon();

        if (_dragSourceIndex < 0)
            return;

        int source = _dragSourceIndex;
        _dragSourceIndex = -1;

        // 命中检测：找鼠标下第一个装备槽或背包格子
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult r in results)
        {
            EquipmentSlot equip = r.gameObject.GetComponentInParent<EquipmentSlot>();
            if (equip != null)
            {
                TryEquip(source, equip.Index);
                RefreshUI();
                return;
            }

            BagSlot bag = r.gameObject.GetComponentInParent<BagSlot>();
            if (bag != null && bag.Index != source)
            {
                TryMove(source, bag.Index);
                RefreshUI();
                return;
            }
        }

        RefreshUI(); // 未命中有效目标：物品放回原处
    }

    // ── 悬停提示（由 BagSlot / EquipmentSlot 转发调用）──

    public void ShowBagTooltip(int bagIndex, Vector2 screenPosition) => ShowTooltip(GetItem(bagIndex), screenPosition);

    public void ShowEquipmentTooltip(int equipIndex, Vector2 screenPosition)
        => ShowTooltip(GetEquipItem(equipIndex), screenPosition);

    public void HideTooltip()
    {
        if (_tooltipRect != null)
            _tooltipRect.gameObject.SetActive(false);
    }

    // ── 内部逻辑（数据读写统一走 InventoryManager）──

    private ItemData GetItem(int bagIndex)
        => InventoryManager.Instance != null ? InventoryManager.Instance.GetBagItem(bagIndex) : null;

    private ItemData GetEquipItem(int equipIndex)
        => InventoryManager.Instance != null ? InventoryManager.Instance.GetEquipItem(equipIndex) : null;

    /// <summary>交换两个背包格子的物品</summary>
    private void TryMove(int from, int to)
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.MoveItem(from, to);
    }

    /// <summary>把背包物品装备到指定槽位（需部位匹配；与槽位原有装备交换）</summary>
    private void TryEquip(int bagIndex, int equipIndex)
    {
        ItemData item = GetItem(bagIndex);
        if (item == null) return;
        if (equipIndex < 0 || equipIndex >= _equipmentSlots.Length) return;
        if (item.equipSlot == EquipmentSlotType.None) return;                            // 不可装备物品
        if (_equipmentSlots[equipIndex].SlotType != item.equipSlot) return;              // 部位不匹配

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.EquipItem(bagIndex, equipIndex);
    }

    private void RefreshUI()
    {
        int displayedCount = 0;
        for (int i = 0; i < _bagSlots.Length; i++)
        {
            ItemData item = GetItem(i);
            if (item != null) displayedCount++;
            _bagSlots[i]?.SetItem(item != null ? item.icon : null);
        }

        for (int i = 0; i < _equipmentSlots.Length; i++)
        {
            ItemData item = GetEquipItem(i);
            _equipmentSlots[i]?.SetItem(item != null ? item.icon : null);
        }

        Debug.Log($"[BagPanelController] RefreshUI：背包格 {_bagSlots.Length} 个，读到物品 {displayedCount} 件，InventoryManager {(InventoryManager.Instance != null ? "存在" : "为 null")}");
    }

    private void ShowTooltip(ItemData item, Vector2 screenPosition)
    {
        if (item == null)
        {
            HideTooltip();
            return;
        }

        if (_tooltipRect == null) return;

        if (_tooltipName != null)
            _tooltipName.text = item.itemName;
        if (_tooltipDesc != null)
            _tooltipDesc.text = item.description;

        _tooltipRect.gameObject.SetActive(true);
        MoveRectToScreenPoint(_tooltipRect, screenPosition, _tooltipOffset);
    }

    /// <summary>把拖拽图标移动到指定屏幕坐标</summary>
    private void MoveDragIcon(Vector2 screenPosition)
    {
        if (_dragIcon == null || !_dragIcon.gameObject.activeSelf) return;
        MoveRectToScreenPoint(_dragIcon.rectTransform, screenPosition, Vector2.zero);
    }

    /// <summary>把 RectTransform 定位到指定屏幕坐标（相对 Canvas，处理 Overlay/Camera 两种模式）</summary>
    private void MoveRectToScreenPoint(RectTransform rect, Vector2 screenPosition, Vector2 offset)
    {
        if (_canvasRect == null) return;

        Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, cam, out Vector2 localPos))
            rect.localPosition = localPos + offset;
    }

    private void HideDragIcon()
    {
        if (_dragIcon != null)
            _dragIcon.gameObject.SetActive(false);
    }
}
