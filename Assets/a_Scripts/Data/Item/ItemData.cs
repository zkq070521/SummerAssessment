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

    [Header("=== 装备加成（战斗时叠加到角色属性）===")]
    public float maxHPBonus;        // 生命值上限加成
    public float attackBonus;       // 攻击（伤害）加成
    public float defenseBonus;      // 防御加成
    public float critRateBonus;     // 暴击率加成（0.1 = +10%）
    public float critDamageBonus;   // 暴击伤害加成（1.5 = +1.5 倍）
    public float speedBonus;        // 速度加成（影响行动顺序）
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
