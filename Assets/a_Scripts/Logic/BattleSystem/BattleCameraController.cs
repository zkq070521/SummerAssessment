using Cinemachine;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 战斗摄像机控制器 — 使用 Cinemachine + DOTween 实现多状态平滑切换
/// 通过 BattleEventCenter 响应战斗事件，完全解耦
/// </summary>
public class BattleCameraController : MonoBehaviour
{
    [Header("Cinemachine 虚拟相机")]
    public CinemachineVirtualCamera battleCam;    // 战斗主虚拟相机

    [Header("入场动画")]
    public float entranceDuration = 2.0f;
    public float entranceZoomFactor = 1.15f;      // 入场时拉近倍数

    [Header("聚焦偏移")]
    public Vector3 playerFocusOffset = new Vector3(-2f, 6f, -8f);   // 玩家回合相机偏移
    public Vector3 enemyFocusOffset = new Vector3(2f, 6f, 8f);      // 敌人回合相机偏移
    public float focusTransitionDuration = 0.8f;

    [Header("震动效果")]
    public float defaultShakeIntensity = 1.5f;
    public float defaultShakeDuration = 0.3f;
    public float shakeFrequency = 2.0f;

    [Header("战斗结束")]
    public float endZoomOutDistance = 5f;          // 结束时拉远距离
    public float endTransitionDuration = 1.5f;

    // 内部状态
    private Transform _camTransform;
    private Vector3 _originLocalPos;
    private float _originOrthoSize;
    // 用于 DOVirtual 的临时变量
    private float _currentFov;
    private CinemachineTransposer _transposer;
    private CinemachineComposer _composer;

    // ──────────── 生命周期 ────────────

    private void Awake()
    {
        if (battleCam == null)
        {
            Debug.LogError("[BattleCamera] battleCam 未赋值！");
            enabled = false;
            return;
        }

        _camTransform = battleCam.transform;
        _transposer = battleCam.GetCinemachineComponent<CinemachineTransposer>();
        _composer = battleCam.GetCinemachineComponent<CinemachineComposer>();

        // 记录初始位置
        _originLocalPos = _camTransform.localPosition;

        // 如果是透视模式，记录 FOV
        _currentFov = battleCam.m_Lens.FieldOfView;
    }

    private void OnEnable()
    {
        BattleEventCenter.OnTurnChanged += OnTurnChangedHandler;
        BattleEventCenter.OnCameraShake += OnCameraShakeHandler;
        BattleEventCenter.OnBattleStart += OnBattleStartHandler;
        BattleEventCenter.OnBattleEnd += OnBattleEndHandler;
        BattleEventCenter.OnUnitHit += OnUnitHitHandler;
    }

    private void OnDisable()
    {
        BattleEventCenter.OnTurnChanged -= OnTurnChangedHandler;
        BattleEventCenter.OnCameraShake -= OnCameraShakeHandler;
        BattleEventCenter.OnBattleStart -= OnBattleStartHandler;
        BattleEventCenter.OnBattleEnd -= OnBattleEndHandler;
        BattleEventCenter.OnUnitHit -= OnUnitHitHandler;
    }

    // ──────────── 事件响应 ────────────

    private void OnBattleStartHandler()
    {
        Debug.Log("[BattleCamera] 战斗开始，播放入场动画");
    }

    private void OnTurnChangedHandler(BattleTeam team)
    {
        switch (team)
        {
            case BattleTeam.Player:
                FocusOnPlayerSide();
                break;
            case BattleTeam.Enemy:
                FocusOnEnemySide();
                break;
        }
    }

    private void OnCameraShakeHandler(float intensity, float duration)
    {
        PlayShakeEffect(intensity, duration);
    }

    private void OnBattleEndHandler(BattleResult result)
    {
        PlayBattleEndEffect(result);
    }

    private void OnUnitHitHandler(BattleUnit target)
    {
        // 每次受击产生轻微震动
        PlayShakeEffect(defaultShakeIntensity * 0.5f, defaultShakeDuration * 0.5f);
    }

    // ──────────── 入场动画 ────────────


    // ──────────── 回合聚焦 ────────────

    /// <summary>
    /// 聚焦到玩家一侧
    /// </summary>
    public void FocusOnPlayerSide()
    {
        if (_transposer == null) return;
        MoveTransposerOffset(playerFocusOffset, focusTransitionDuration);
    }

    /// <summary>
    /// 聚焦到敌人一侧
    /// </summary>
    public void FocusOnEnemySide()
    {
        if (_transposer == null) return;
        MoveTransposerOffset(enemyFocusOffset, focusTransitionDuration);
    }

    private void MoveTransposerOffset(Vector3 targetOffset, float duration)
    {
        DOTween.Kill(this, true);
        Vector3 startOffset = _transposer.m_FollowOffset;

        DOTween.To(() => startOffset, value =>
        {
            startOffset = value;
            _transposer.m_FollowOffset = value;
        }, targetOffset, duration)
        .SetEase(Ease.InOutQuad)
        .SetId(this);
    }

    // ──────────── 震动效果 ────────────

    /// <summary>
    /// 播放摄像机震动
    /// </summary>
    public void PlayShakeEffect(float intensity, float duration)
    {
        if (battleCam == null) return;

        // 使用 DOTween 模拟 Noise
        transform.DOShakePosition(duration, intensity * 0.1f, 10, shakeFrequency, false, true)
            .SetId(this);
        transform.DOShakeRotation(duration, intensity * 2f, 10, shakeFrequency)
            .SetId(this);
    }

    /// <summary>
    /// 播放指定强度的震动（公开方法，供外部直接调用）
    /// </summary>
    public void PlayShake(float intensity = -1f, float duration = -1f)
    {
        float i = intensity > 0 ? intensity : defaultShakeIntensity;
        float d = duration > 0 ? duration : defaultShakeDuration;
        PlayShakeEffect(i, d);
    }

    // ──────────── 战斗结束 ────────────

    /// <summary>
    /// 播放战斗结束摄像机效果
    /// </summary>
    public void PlayBattleEndEffect(BattleResult result)
    {
        if (battleCam == null) return;

        DOTween.Kill(this, true);

        switch (result)
        {
            case BattleResult.Victory:
                // 胜利：拉远展示全局
                DOTween.To(() => _currentFov, value =>
                {
                    _currentFov = value;
                    battleCam.m_Lens.FieldOfView = value;
                }, _currentFov + endZoomOutDistance, endTransitionDuration)
                .SetEase(Ease.OutCubic)
                .SetId(this);
                break;

            case BattleResult.Defeat:
                // 失败：缓慢拉近 + 轻微晃动
                DOTween.To(() => _currentFov, value =>
                {
                    _currentFov = value;
                    battleCam.m_Lens.FieldOfView = value;
                }, _currentFov - 10f, endTransitionDuration)
                .SetEase(Ease.InCubic)
                .SetId(this);
                PlayShakeEffect(defaultShakeIntensity * 0.8f, endTransitionDuration);
                break;
        }
    }

    // ──────────── 重置 ────────────

    /// <summary>
    /// 重置摄像机到初始状态
    /// </summary>
    public void ResetCamera()
    {
        DOTween.Kill(this);
        _camTransform.localPosition = _originLocalPos;
        battleCam.m_Lens.FieldOfView = _currentFov;
        if (_transposer != null)
            _transposer.m_FollowOffset = Vector3.zero;
    }
}
