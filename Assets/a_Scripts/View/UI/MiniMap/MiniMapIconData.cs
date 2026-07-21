using UnityEngine;

/// <summary>
/// 小地图标记类型枚举，决定标记的图标样式
/// - Player: 玩家当前位置与朝向（三角箭头，自动旋转）
/// - NPC: 可交互 NPC 位置
/// - QuestTarget: 当前任务目标位置
/// - TeleportPoint: 传送锚点位置
/// - Enemy: 敌人位置（战斗场景用）
/// </summary>
public enum MiniMapIconType
{
    Player,
    NPC,
    QuestTarget,
    TeleportPoint,
    Enemy
}

/// <summary>
/// 小地图标记的运行时数据，从场景物体映射到小地图 UI 坐标
/// </summary>
[System.Serializable]
public struct MiniMapIconEntry
{
    /// <summary>世界空间中的跟踪目标 Transform</summary>
    public Transform target;
    /// <summary>标记类型</summary>
    public MiniMapIconType iconType;
    /// <summary>对应的 UI RectTransform（由 MiniMapManager 动态创建）</summary>
    [System.NonSerialized] public RectTransform uiElement;
}
