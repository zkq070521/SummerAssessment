using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包格子 — 右侧 4×4 网格中的一个格子。
/// 显示物品图标，并把拖拽 / 悬停输入转发给 BagPanelController（本组件不持有数据）。
///
/// 结构：本物体需带一个 Image 作为背景（raycastTarget 开启，用于接收拖拽与悬停），
///       子物体放物品图标 Image（raycastTarget 关闭，避免遮挡）。
/// </summary>
public class BagSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;   // 物品图标（子物体，无物品时隐藏）

    /// <summary>格子索引（0~15），由 BagPanelController.Initialize 赋值</summary>
    public int Index { get; private set; }

    private BagPanelController _controller;

    public void Initialize(int index, BagPanelController controller)
    {
        Index = index;
        _controller = controller;
    }

    /// <summary>显示物品图标；icon 为 null 时隐藏图标（空格子）</summary>
    public void SetItem(Sprite icon)
    {
        if (_icon == null) return;
        _icon.sprite = icon;
        _icon.gameObject.SetActive(icon != null);
    }

    // ── 拖拽与悬停：全部转发给控制器 ──

    public void OnBeginDrag(PointerEventData eventData) => _controller?.BeginDrag(Index, eventData.position);
    public void OnDrag(PointerEventData eventData) => _controller?.Drag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => _controller?.EndDrag(eventData);
    public void OnPointerEnter(PointerEventData eventData) => _controller?.ShowBagTooltip(Index, eventData.position);
    public void OnPointerExit(PointerEventData eventData) => _controller?.HideTooltip();
}
