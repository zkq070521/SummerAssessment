using UnityEngine;

/// <summary>
/// 战斗实体数据 — 运行时战斗单位的纯数据容器（与视觉表现完全解耦）
///
/// 职责：存储 HP / 速度 / 攻击等核心战斗属性，供 TurnManager 排序和 BattleManager 伤害计算使用。
/// 表现层字段（音效、特效、模型、技能引用）不存放于此，需要时直接从 HeroData ScriptableObject 读取。
/// </summary>
[System.Serializable]
public class BattleEntityData
{
    [Header("=== 身份 ===")]
    public string heroName;                 // 角色名称（日志 / UI 显示）
    public string heroID;                   // 唯一 ID（存档 / 查找）
    public BattleTeam team;                 // 所属队伍（Player / Enemy）
    public GameObject battlePrefab;         // 战斗用预制体（仅用于敌人 Spawn；玩家从 HeroData 读取）
    public EnemyAnimationConfig animationConfig;  // 敌人动画资产配置（待机/攻击/受击/死亡，供 EnemyBattleAnimator 播放）

    [Header("=== 战斗属性 ===")]
    public float maxHP = 25000f;
    public float currentHP;
    public float attack = 3000f;
    public float defense = 300f;
    public float speed = 110f;              // 决定行动顺序（AV = 10000 / speed）
    public float critRate = 0.1f;           // 暴击率
    public float critDamage = 1.5f;         // 暴击倍率

    [Header("=== 技能倍率 ===")]
    public float basicAttackMultiplier = 1f;   // 普攻伤害倍率（攻击力 × 倍率）
    public float skillMultiplier = 2f;         // 战技伤害倍率
    public float ultimateMultiplier = 3f;      // 终结技伤害倍率

    [Header("=== 能量 ===")]
    public float maxEnergy = 100f;
    public float currentEnergy;

    [Header("=== 元素与命途 ===")]
    public ElementType element;             // 元素类型（预留元素克制系统）
    public PathType path;                   // 命途类型

    [Header("=== UI 资源 ===")]
    public Sprite icon;                     // 行动顺序条头像（敌人由 Inspector 拖入；玩家从 HeroData 拷贝）

    [Header("=== 状态 ===")]
    public bool isAlive = true;

    /// <summary>
    /// 深拷贝（用于从敌人模板创建运行时实例）
    /// </summary>
    public BattleEntityData Clone()
    {
        return new BattleEntityData
        {
            heroName = this.heroName,
            heroID = this.heroID,
            team = this.team,
            battlePrefab = this.battlePrefab,
            animationConfig = this.animationConfig,
            maxHP = this.maxHP,
            currentHP = this.maxHP,
            attack = this.attack,
            defense = this.defense,
            speed = this.speed,
            critRate = this.critRate,
            critDamage = this.critDamage,
            basicAttackMultiplier = this.basicAttackMultiplier,
            skillMultiplier = this.skillMultiplier,
            ultimateMultiplier = this.ultimateMultiplier,
            maxEnergy = this.maxEnergy,
            currentEnergy = this.maxEnergy,
            element = this.element,
            path = this.path,
            icon = this.icon,
            isAlive = this.isAlive
        };
    }

    /// <summary>受到伤害，HP 归零时自动标记死亡</summary>
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        if (currentHP <= 0)
            isAlive = false;
    }

    /// <summary>恢复 HP</summary>
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        isAlive = true;
    }
}
