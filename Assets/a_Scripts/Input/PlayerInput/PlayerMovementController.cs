using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 第三人称角色移动控制器
/// 使用 Input System（生成类 InputController），CharacterController.Move 驱动
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("移动")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("旋转")]
    public float rotationSmoothTime = 0.12f;

    [Header("重力")]
    public float gravity = -9.81f;

    [Header("参考")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("攻击")]
    public GameObject weaponObject;              // 角色手中的武器物体
    public float dashSpeed = 8f;                 // 攻击前冲速度
    public float dashDuration = 0.25f;           // 前冲持续时间
    public float dashRotationAngle = 90f;        // 前冲期间绕 Y 轴旋转角度（逆时针为正）
    public bool needAttackRotation = true;       // 是否需要攻击旋转（区分角色类型）
    public float returnRotationDuration = 0.6f;  // 转回原方向的耗时（建议比 dashDuration 长，更自然）
    public float weaponHideDelay = 1.8f;         // 武器显示后隐藏延迟
    public ParticleSystem slashEffect;           // 刀光粒子系统（Burst 型，初始失活，播放时激活）
    public ParticleSystem trailEffect;           // 武器拖尾粒子系统（持续型，初始失活，播放时激活）
    public ParticleSystem hideEffect;            // 武器隐藏粒子系统（Burst 型，初始失活，播放时激活）
    public float trailDuration = 0.5f;           // 拖尾持续时间（秒）

    // 组件
    private CharacterController _controller;
    private Transform _transform;
    public InputController _input;

    // 状态
    private Vector2 _moveInput;
    private bool _isSprinting;
    private float _rotationVelocity;
    private float _verticalVelocity;

    // 输入封锁
    private bool _inputBlocked;

    // 攻击
    private InputAction _attackAction;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private bool _isAttacking;
    private Coroutine _attackCoroutine;

    // 特效协程管理（统一管理刀光/拖尾的激活→播放→失活生命周期）
    private Coroutine _effectCoroutine;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _transform = transform;
        _input = new InputController();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (animator == null)
            animator = GetComponent<Animator>();

        // 攻击输入：鼠标右键
        _attackAction = _input.Player.PlayerAttack;
        _attackAction.performed += OnAttack;
    }

    void Start()
    {
        LockMouse();
    }

    void OnEnable()
    {
        _input.Player.Enable();
        _attackAction?.Enable();
        // InputService.OnInputEnabledChanged += OnInputEnabledChanged;
    }

    void OnDisable()
    {
        _input.Player.Disable();
        _attackAction?.Disable();
        // InputService.OnInputEnabledChanged -= OnInputEnabledChanged;
    }

    void OnDestroy()
    {
        _attackAction?.Dispose();
    }

    private void OnInputEnabledChanged(bool enabled)
    {
        _inputBlocked = !enabled;
    }

    void Update()
    {
        // ESC 切换鼠标锁定 / 解锁
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                UnlockMouse();
            else
                LockMouse();
        }

        if (_inputBlocked || _isAttacking) return;

        // 读取移动输入
        _moveInput = _input.Player.PlayerMove.ReadValue<Vector2>();

        // Sprint 切换：按下时翻转状态
        if (_input.Player.Sprint.WasPressedThisFrame())
            _isSprinting = !_isSprinting;

        // 计算移动方向（相对镜头）
        Vector3 moveDir = CalculateMoveDirection();

        // 移动 + 重力
        ApplyMovement(moveDir);
        ApplyGravity();

        // 旋转朝向
        ApplyRotation(moveDir);

        // 动画
        UpdateAnimation();
    }

    private Vector3 CalculateMoveDirection()
    {
        if (_moveInput == Vector2.zero) return Vector3.zero;

        float yaw = cameraTransform != null ? cameraTransform.eulerAngles.y : _transform.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 forward = rot * Vector3.forward;
        Vector3 right = rot * Vector3.right;
        forward.y = 0f; forward.Normalize();
        right.y = 0f; right.Normalize();

        return (forward * _moveInput.y + right * _moveInput.x).normalized;
    }

    private void ApplyMovement(Vector3 moveDir)
    {
        float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;

        // 直接计算目标速度
        Vector3 targetVelocity = moveDir * targetSpeed;
        targetVelocity.y = _verticalVelocity;

        // 直接移动
        _controller.Move(targetVelocity * Time.deltaTime);
    }
    private void ApplyRotation(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero)
        {
            _rotationVelocity = 0f;
            return;
        }

        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(_transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
        _transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -0.2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
        if (_moveInput == Vector2.zero) speed = 0f;

        animator.SetFloat("Speed", speed);
        animator.SetBool("isRunning", _isSprinting);
    }

    #region 鼠标锁定

    private void LockMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion

    #region 攻击

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (_inputBlocked || _isAttacking) return;
        if (animator == null) return;

        // 防止协程冲突：如果已有攻击协程在运行，先停止
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }
        _attackCoroutine = StartCoroutine(AttackSequence());
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        _isAttacking = true;

        // 1. 播放攻击动画
        animator.SetTrigger(AttackHash);

        // 4. 显示武器
        if (weaponObject != null)
        {
            weaponObject.SetActive(true);

            // 等待一帧，确保武器和粒子系统 GameObject 完全激活后再播放粒子
            yield return null;

            PlaySlashEffect();  // 播放刀光效果
            PlayTrailEffect();  // 播放武器拖尾效果
        }
        _isAttacking = false;

        // 2. 前冲：朝角色面朝方向快速移动
        Vector3 dashDir = _transform.forward;
        dashDir.y = 0f;
        dashDir.Normalize();

        float timer = 0f;
        while (timer < dashDuration)
        {
            _controller.Move(dashDir * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // 3. 攻击旋转（仅需要旋转的角色）
        Quaternion originalRotation = _transform.rotation; // 保存旋转前的朝向
        if (needAttackRotation)
        {
            float rotateSpeed = dashRotationAngle / dashDuration;
            timer = 0f;
            while (timer < dashDuration)
            {
                float rotateAmount = rotateSpeed * Time.deltaTime;
                _transform.Rotate(Vector3.up, rotateAmount);
                timer += Time.deltaTime;
                yield return null;
            }
        }



        // 5. 延迟后隐藏武器
        yield return new WaitForSeconds(weaponHideDelay);

        if (weaponObject != null)
        {
            weaponObject.SetActive(false);
            hideEffect?.Play(); // 播放武器隐藏粒子效果
        }




        // 6. 转回原来方向（仅需要旋转的角色，使用 Slerp 平滑过渡）
        if (needAttackRotation)
        {
            Quaternion startRotation = _transform.rotation;
            timer = 0f;
            while (timer < returnRotationDuration)
            {
                float t = timer / returnRotationDuration;
                // 使用 SmoothStep 让旋转有缓入缓出效果
                float easedT = t * t * (3f - 2f * t);
                _transform.rotation = Quaternion.Slerp(startRotation, originalRotation, easedT);
                timer += Time.deltaTime;
                yield return null;
            }
            _transform.rotation = originalRotation; // 确保最终精确到位
        }
    }

    /// <summary>
    /// 播放刀光粒子（Burst 型，初始失活 → 激活 → 播放 → 自然结束）
    /// </summary>
    public void PlaySlashEffect()
    {
        if (slashEffect == null)
        {
            Debug.LogWarning("[PlaySlashEffect] slashEffect 未赋值！请在 Inspector 中拖入粒子系统。");
            return;
        }

        // 1. 激活粒子 GameObject（初始为失活状态）
        if (!slashEffect.gameObject.activeSelf)
            slashEffect.gameObject.SetActive(true);

        // 2. 关闭 PlayOnAwake，防止自动播放干扰手动控制
        if (slashEffect.main.playOnAwake)
        {
            var main = slashEffect.main;
            main.playOnAwake = false;
        }

        // 3. Clear + time=0 + Play 重新触发 Burst（非循环系统的可靠重启方式）
        slashEffect.Clear();
        slashEffect.time = 0;
        slashEffect.Play();
    }

    /// <summary>
    /// 播放武器拖尾粒子（持续型，初始失活 → 激活 → 播放 → trailDuration 秒后停止并失活）
    /// </summary>
    public void PlayTrailEffect()
    {
        // 1. 防止协程冲突：如果已有特效协程在运行，先停止
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }

        // 2. 防御性检查
        if (trailEffect == null)
        {
            Debug.LogWarning("[PlayTrailEffect] trailEffect 未赋值！请在 Inspector 中拖入粒子系统。");
            return;
        }

        // 3. 激活粒子 GameObject（初始为失活状态）
        if (!trailEffect.gameObject.activeSelf)
            trailEffect.gameObject.SetActive(true);

        // 4. 确保拖尾粒子是循环模式
        var main = trailEffect.main;
        if (!main.loop)
            main.loop = true;

        // 5. 清理残留粒子，重置时间，开始播放
        trailEffect.Clear();
        trailEffect.time = 0;
        trailEffect.Play();

        // 6. 启动统一特效清理协程（同时管理刀光和拖尾的失活）
        _effectCoroutine = StartCoroutine(DeactivateEffectsRoutine());
    }

    /// <summary>
    /// 统一特效清理协程：同时停止刀光和拖尾粒子并失活它们的 GameObject
    /// </summary>
    private IEnumerator DeactivateEffectsRoutine()
    {
        yield return new WaitForSeconds(trailDuration);

        // 停止并失活拖尾粒子
        if (trailEffect != null)
        {
            trailEffect.Stop();
            trailEffect.gameObject.SetActive(false);
        }

        // 停止并失活刀光粒子
        if (slashEffect != null)
        {
            slashEffect.Stop();
            slashEffect.gameObject.SetActive(false);
        }

        _effectCoroutine = null;
    }


    #endregion

    // 这个方法会在动画最后一帧被调用
    public void OnAnimationEnd()
    {
        // 把速度设为0，停在最后一帧
        animator.speed = 0f;

        // 2秒后恢复
        StartCoroutine(ResumeAfterDelay());
    }

    IEnumerator ResumeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        animator.speed = 1f;
    }

}
