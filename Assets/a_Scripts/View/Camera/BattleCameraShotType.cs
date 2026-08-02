/// <summary>
/// 战斗镜头类型枚举
///
/// 定义回合制战斗中所有可用的摄像机机位类型。
/// 当前 BattleCameraController 直接管理优先级切换，
/// 此枚举预留供未来重构为 SwitchToShot(BattleCameraShotType) 统一接口使用。
/// </summary>
public enum BattleCameraShotType
{
    /// <summary>无（未激活）</summary>
    None = 0,

    /// <summary>广角待机镜头 — 回合间隙 / 选技能时使用，双方队伍均可见</summary>
    IdleWide = 1,

    /// <summary>角色回合特写 — 当前行动角色侧前方低角度构图</summary>
    CharacterFocus = 2,

    /// <summary>攻击镜头 — 攻击者身后 / 侧面，构图同时包含攻击者和目标</summary>
    Attack = 3,

    /// <summary>入场镜头阶段一 — 战斗开始的第一个镜头</summary>
    Entrance1 = 4,

    /// <summary>入场镜头阶段二 — 过渡到战斗广角的中间镜头</summary>
    Entrance2 = 5,

    /// <summary>胜利镜头（预留）</summary>
    Victory = 6,

    /// <summary>终结技特写（预留）— 角色正面仰拍 + 大光圈效果</summary>
    Ultimate = 7,

    /// <summary>阵亡镜头（预留）— 单位死亡时的慢动作特写</summary>
    Death = 8,
}
