using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New HeroData", menuName = "Game/HeroData")]
public class HeroData : ScriptableObject
{
    [Header("=== 基础信息 ===")]
    public string heroName;              // 角色名字
    public string heroID;               // 唯一ID，用于存档和查找
    [TextArea(3, 5)] public string description; // 角色描述

    [Header("=== 模型与表现 ===")]
    public GameObject modelPrefab;      // 角色的3D模型预制体（含Animator）
    public GameObject battlePrefab;     // 战斗场景中的角色预制体

    [Header("=== 属性 ===")]
    public float maxHP = 1000f;
    public float currentHP;             // 当前血量（运行时修改，不保存到资产）
    public float attack = 100f;
    public float defense = 50f;
    public float speed = 110f;          // 决定行动顺序

    [Header("=== 元素与命途 ===")]
    public ElementType element;         // 物理/火/冰/雷/风/虚数/量子
    public PathType path;              // 巡猎/智识/毁灭/同谐/虚无/存护/丰饶

    [Header("=== 技能系统 ===")]
    public SkillData basicAttack;       // 普攻
    public SkillData skill;            // 战技
    public SkillData ultimate;         // 终结技
    public SkillData talent;           // 天赋（被动）
    public SkillData technique;        // 秘技（大世界使用）

    [Header("=== UI资源 ===")]
    public Sprite icon;                // 角色头像（小队界面）
    public Sprite portrait;            // 角色立绘（详情界面）
    public Sprite ultimateIcon;       // 终结技图片立绘 
    public Sprite skillIconBasic;      // 普攻按钮图标
    public Sprite skillIconSkill;      // 战技按钮图标
    public Sprite skillIconUltimate;   // 终结技按钮图标
    public Sprite elementIcon;         // 元素图标（UI上显示）

    [Header("=== 声音资源 ===")]
    public AudioClip battleStartVoice;  // 战斗入场语音
    public AudioClip skillVoice;        // 释放战技语音
    public AudioClip ultimateVoice;     // 释放终结技语音
    public AudioClip hitVoice;          // 受击语音
    public AudioClip dieVoice;          // 阵亡语音

    [Header("=== 特效资源 ===")]
    public GameObject skillEffect;      // 战技特效预制体
    public GameObject ultimateEffect;   // 终结技特效预制体
    public GameObject hitEffect;        // 受击特效

    [Header("=== 能量系统 ===")]
    public float maxEnergy = 100f;          // 能量上限

    // 运行时数据（不会保存到资产文件）
    [System.NonSerialized] public float currentHPNotSave;  // 当前血量
    [System.NonSerialized] public float currentEnergy;     // 当前能量
    [System.NonSerialized] public bool isAlive = true;

    // 初始化运行时数据
    public void InitializeRuntimeData()
    {
        currentHP = maxHP;
        currentEnergy = maxEnergy;
        isAlive = true;
    }
}

// 元素类型枚举
public enum ElementType
{
    Physical,   // 物理
    Fire,       // 火
    Ice,        // 冰
    Lightning,  // 雷
    Wind,       // 风
    Quantum,    // 量子
    Imaginary   // 虚数
}

// 命途类型枚举
public enum PathType
{
    Hunt,       // 巡猎
    Erudition,  // 智识
    Destruction,// 毁灭
    Harmony,    // 同谐
    Nihility,   // 虚无
    Preservation,// 存护
    Abundance   // 丰饶
}