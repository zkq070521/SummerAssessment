using UnityEngine;

/// <summary>
/// 物品配置（ScriptableObject）— 定义一件物品的名称、图标、简介，以及可装备的部位。
///
/// 通过 Assets → Create → Game/ItemData 创建资产，再拖入 InventoryManager 的初始物品列表，
/// 或在运行时用 InventoryManager.AddItem() / 对话奖励加入背包。
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Game/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;                        // 物品名称
    [TextArea(2, 5)] public string description;    // 物品简介（悬停时显示）
    public Sprite icon;                            // 物品图标
    public EquipmentSlotType equipSlot;            // 可装备的部位（None 表示不可装备的普通物品）
}

/// <summary>
/// 装备部位 — 头 + 四肢，共 5 个槽位；None 表示不可装备。
/// 用于 ItemData.equipSlot 与 EquipmentSlot.slotType 的匹配判断。
/// </summary>
public enum EquipmentSlotType
{
    None = 0,       // 不可装备
    Head = 1,       // 头部
    LeftArm = 2,    // 左臂
    RightArm = 3,   // 右臂
    LeftLeg = 4,    // 左腿
    RightLeg = 5    // 右腿
}
