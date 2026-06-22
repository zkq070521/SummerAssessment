using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Game/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea(2, 4)] public string description;

    public SkillType skillType;         // 普攻/战技/终结技/天赋/秘技
    public TargetType targetType;       // 单体/群体/自身/友方

    public float damageMultiplier = 1f; // 伤害倍率
    public float energyCost = 0f;       // 能量消耗（战技）
    public float energyGain = 20f;      // 回复能量（普攻）
    public int skillPointCost = 0;      // 战技点消耗（崩铁特色）
    public int maxTargets = 1;          // 最大目标数

    public GameObject effectPrefab;     // 技能特效
    public AudioClip soundEffect;       // 技能音效
    public Sprite icon;                // 技能图标（如果和角色特有的不同）

    // 特殊效果（用Unity的Event或自定义系统）
    public bool hasSpecialEffect;
    public string specialEffectID;     // 对应到代码里的特殊效果ID
}

public enum SkillType
{
    BasicAttack,
    Skill,
    Ultimate,
    Talent,
    Technique
}

public enum TargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    Ally,
    AllAllies
}