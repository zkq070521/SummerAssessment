using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// 单个行动顺序槽位 — 背景板（父 Image）与头像（子 Image）成对引用。
    ///
    /// 动效作用于背景板（父节点），头像作为子物体随背景板整体移动 / 缩放。
    /// 背景板负责展示槽位底板，头像负责展示角色 Sprite。
    /// </summary>
    [System.Serializable]
    public class TurnOrderSlot
    {
        public Image background;   // 背景板（参与滑动 / 缩放动画）
        public Image icon;         // 角色头像（显示 sprite）
    }
}
