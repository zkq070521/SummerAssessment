using DG.Tweening;
using UnityEngine;

/// <summary>
/// 战斗单位 — 挂载在生成的角色模型上
/// 管理战斗中的视觉表现（受击、攻击、死亡）
/// 通过 BattleEventCenter 响应事件，不直接耦合状态机
/// </summary>
[RequireComponent(typeof(Animator))]
public class BattleUnit : MonoBehaviour
{
    [Header("组件引用")]
    public Animator animator;
    public Renderer unitRenderer;   // 用于闪白效果的主渲染器（可拖拽或自动获取）

    [Header("受击效果")]
    public float hitFlashDuration = 0.15f;
    public float hitShakeDuration = 0.2f;
    public float hitShakeStrength = 0.3f;

    [Header("攻击前冲")]
    public float attackLungeDistance = 1.5f;
    public float attackLungeDuration = 0.2f;
    public float attackReturnDuration = 0.2f;

    [Header("死亡效果")]
    public float deathFadeDuration = 0.8f;
    public float deathRiseHeight = 0.5f;

    // ── 运行时数据 ──
    public BattleEntityData EntityData { get; private set; }
    public BattleTeam Team { get; set; }
    public bool IsAlive => EntityData != null && EntityData.isAlive;

    // 内部状态
    private Vector3 _originPos;
    private Color _originColor;
    private bool _isPlayingAction;
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    // ──────────── 生命周期 ────────────

    // 材质颜色属性名（URP 使用 _BaseColor，Built-in 使用 _Color）
    private static readonly string ColorProperty = "_BaseColor";

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (unitRenderer == null) unitRenderer = GetComponentInChildren<Renderer>();

        _originPos = transform.position;

        // 兼容 URP（_BaseColor）和 Built-in（_Color）
        Material mat = unitRenderer?.material;
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor"))
                _originColor = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
            {
                _originColor = mat.color;
                //ColorProperty = "_Color";
            }
        }
    }

    private void OnEnable()
    {
        BattleEventCenter.OnDamageDealt += OnDamageDealtHandler;
        BattleEventCenter.OnUnitAttack += OnUnitAttackHandler;
    }

    private void OnDisable()
    {
        BattleEventCenter.OnDamageDealt -= OnDamageDealtHandler;
        BattleEventCenter.OnUnitAttack -= OnUnitAttackHandler;
    }

    // ──────────── 初始化 ────────────

    /// <summary>
    /// 用数据初始化该单位
    /// </summary>
    public void Setup(BattleEntityData data, BattleTeam team)
    {
        EntityData = data;
        Team = team;
        _originPos = transform.position;

        gameObject.name = $"{team}_{data.entityName}";

        // 设置朝向
        if (team == BattleTeam.Player)
            transform.forward = Vector3.right;   // 面向右侧（敌人方向）
        else
            transform.forward = Vector3.left;    // 面向左侧（玩家方向）
    }

    // ──────────── 事件响应 ────────────

    private void OnDamageDealtHandler(BattleUnit source, BattleUnit target, int damage, bool isCritical)
    {
        if (target != this) return;

        // 受击动画
        if (animator != null)
            animator.SetTrigger(HitHash);

        // 闪白 + 震动
        PlayHitFlash();
        PlayHitShake();

        // 触发受击事件（供摄像机响应）
        BattleEventCenter.TriggerUnitHit(this);

        // 检查死亡
        if (!EntityData.isAlive)
            PlayDeathEffect();
    }

    private void OnUnitAttackHandler(BattleUnit source, BattleUnit target)
    {
        if (source != this || _isPlayingAction) return;

        // 播放攻击前冲动画
        _isPlayingAction = true;
        if (animator != null)
            animator.SetTrigger(AttackHash);

        // 计算目标方向的前冲
        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0f;

        Sequence attackSeq = DOTween.Sequence();
        attackSeq.Append(transform.DOMove(transform.position + direction * attackLungeDistance, attackLungeDuration)
            .SetEase(Ease.OutQuad));
        attackSeq.AppendCallback(() =>
        {
            // 前冲到位时触发伤害事件
            // 实际伤害计算由状态机完成
        });
        attackSeq.Append(transform.DOMove(_originPos, attackReturnDuration)
            .SetEase(Ease.InQuad));
        attackSeq.OnComplete(() =>
        {
            _isPlayingAction = false;
        });
    }

    // ──────────── 受击效果 ────────────

    private void PlayHitFlash()
    {
        if (unitRenderer == null) return;

        Material mat = unitRenderer.material;
        if (!mat.HasProperty(ColorProperty)) return;

        // 闪白 (兼容 URP _BaseColor 和 Built-in _Color)
        DOTween.Kill(this, true);
        mat.DOColor(Color.white, ColorProperty, hitFlashDuration)
            .SetId(this)
            .OnComplete(() => mat.DOColor(_originColor, ColorProperty, hitFlashDuration * 0.5f).SetId(this));
    }

    private void PlayHitShake()
    {
        // 原地震动
        transform.DOShakePosition(hitShakeDuration, hitShakeStrength, 10, 90, false, true)
            .SetId(this)
            .OnComplete(() => transform.position = _originPos);
    }

    // ──────────── 死亡效果 ────────────

    private void PlayDeathEffect()
    {
        if (animator != null)
            animator.SetTrigger(DeadHash);

        // 淡出 + 上升
        Sequence deathSeq = DOTween.Sequence();
        deathSeq.Join(transform.DOMoveY(transform.position.y + deathRiseHeight, deathFadeDuration)
            .SetEase(Ease.OutQuad));
        if (unitRenderer != null && unitRenderer.material.HasProperty(ColorProperty))
            deathSeq.Join(unitRenderer.material.DOFade(0f, deathFadeDuration));
        deathSeq.OnComplete(() =>
        {
            BattleEventCenter.TriggerUnitDeath(this);
            gameObject.SetActive(false);
        });
    }

    // ──────────── 公开方法 ────────────

    /// <summary>
    /// 重置位置到初始点（战斗结束后恢复用）
    /// </summary>
    public void ResetPosition()
    {
        transform.position = _originPos;
        if (unitRenderer != null && unitRenderer.material.HasProperty(ColorProperty))
        {
            if (ColorProperty == "_BaseColor")
                unitRenderer.material.SetColor("_BaseColor", _originColor);
            else
                unitRenderer.material.color = _originColor;
        }
    }
}
