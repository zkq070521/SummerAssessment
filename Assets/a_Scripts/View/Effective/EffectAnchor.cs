/// <summary>
/// 特效生成锚点枚举
///
/// 决定 AttackEffectPlayer 实例化的特效预制体在哪个位置生成：
/// - Source: 攻击者位置（通过 BattleManager 的 entity→Transform 映射解析）
/// - Target: 受击目标位置（命中点，适合斩击火花 / 爆点类特效）
/// - Custom: 自定义 Transform（武器尖端 / 手部等挂点，由 Inspector 单独拖入）
/// </summary>
public enum EffectAnchor
{
    /// <summary>攻击者位置 — 特效从出手方身上 / 身边生成</summary>
    Source,

    /// <summary>受击目标位置 — 特效在命中点上生成</summary>
    Target,

    /// <summary>自定义锚点 — 拖入指定的 Transform（武器尖端、手部等挂点）</summary>
    Custom,
}
