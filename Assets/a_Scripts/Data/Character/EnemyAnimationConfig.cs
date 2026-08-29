using UnityEngine;

/// <summary>
/// 敌人动画资产配置 — 集中管理敌人战斗动画的 AnimationClip 引用。
///
/// 与 HeroData 的表现字段（音效/特效）同理，属表现层数据：逻辑层（BattleManager）不依赖动画，
/// 由表现层组件 EnemyBattleAnimator 读取并播放。不同敌人类型各建一份资产即可复用同一套播放逻辑。
///
/// 使用方式：在 Assets/DataSO/ 下 Create → Game → EnemyAnimationConfig，
/// 拖入 待机 / 攻击 / 受击 / 死亡 四个动画片段，再挂到 BattleManager 敌人配置的 animationConfig 字段。
/// </summary>
[CreateAssetMenu(fileName = "New EnemyAnimationConfig", menuName = "Game/EnemyAnimationConfig")]
public class EnemyAnimationConfig : ScriptableObject
{
    [Header("=== 待机动画 ===")]
    public AnimationClip idleAnimation;      // 敌人默认 / 待机状态动画

    [Header("=== 攻击动画 ===")]
    public AnimationClip attackAnimation;    // 敌人主动攻击时播放

    [Header("=== 受击动画 ===")]
    public AnimationClip hitAnimation;       // 敌人受到伤害时播放

    [Header("=== 死亡动画 ===")]
    public AnimationClip dieAnimation;       // 敌人阵亡时播放
}
