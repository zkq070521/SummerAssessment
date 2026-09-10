using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 装备槽 — 面板左侧 5 个槽位（头 + 四肢）。
/// 作为拖拽放置目标（在 BagPanelController.EndDrag 里通过 Raycast 识别），并支持悬停显示装备简介。
///
/// 结构：本物体带 Image 背景（raycastTarget 开启），子物体放装备图标 Image（raycastTarget 关闭）。
/// 在 Inspector 中为每个槽位设置对应的 _slotType。
/// </summary>
public class EquipmentSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private EquipmentSlotType _slotType = EquipmentSlotType.None;   // 本槽位对应的部位
    [SerializeField] private Image _icon;   // 装备图标（子物体，无装备时隐藏）

    /// <summary>槽位索引（0~4），由 BagPanelController.Initialize 赋值</summary>
    public int Index { get; private set; }

    /// <summary>本槽位可接受的装备部位</summary>
    public EquipmentSlotType SlotType => _slotType;

    private BagPanelController _controller;

    public void Initialize(int index, BagPanelController controller)
    {
        Index = index;
        _controller = controller;
    }

    /// <summary>显示装备图标；icon 为 null 时隐藏（空槽位）</summary>
    public void SetItem(Sprite icon)
    {
        if (_icon == null) return;
        _icon.sprite = icon;
        _icon.gameObject.SetActive(icon != null);
    }

    public void OnPointerEnter(PointerEventData eventData) => _controller?.ShowEquipmentTooltip(Index, eventData.position);
    public void OnPointerExit(PointerEventData eventData) => _controller?.HideTooltip();
}
