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
    public float accelerationSmoothTime = 0.1f;
    public float decelerationSmoothTime = 0.2f;

    [Header("旋转")]
    public float rotationSmoothTime = 0.12f;

    [Header("重力")]
    public float gravity = -9.81f;

    [Header("参考")]
    public Transform cameraTransform;
    public Animator animator;

    [Header("攻击")]
    public GameObject weaponObject;           // 角色手中的武器物体
    public float dashSpeed = 8f;             // 攻击前冲速度
    public float dashDuration = 0.25f;        // 前冲持续时间
    public float dashRotationAngle = 90f;   // 前冲期间绕 Y 轴旋转角度（逆时针为正）
    public bool needAttackRotation = true;   // 是否需要攻击旋转（区分角色类型）
    public float returnRotationDuration = 0.6f; // 转回原方向的耗时（建议比 dashDuration 长，更自然）
    public float weaponHideDelay = 1.8f;        // 武器显示后隐藏延迟
    public ParticleSystem slashParticle;  // 拖入你的WeaponTrail粒子系统
    public ParticleSystem trailParticle; // 拖入你的刀光粒子系统

    // 组件
    private CharacterController _controller;
    private Transform _transform;
    public InputController _input;

    // 状态
    private Vector2 _moveInput;
    private bool _isSprinting;
    // private Vector3 _smoothVelocity;
    private float _rotationVelocity;
    private float _verticalVelocity;

    // 输入封锁
    private bool _inputBlocked;

    // 攻击
    private InputAction _attackAction;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private bool _isAttacking;
    private Coroutine _attackCoroutine;

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

        if (_attackCoroutine != null)
            StopCoroutine(_attackCoroutine);
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
            weaponObject.SetActive(false);

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

    // 播放刀光
    public void PlaySlashEffect()
    {
        if (slashParticle == null)
        {
            Debug.LogWarning("[PlaySlashEffect] slashParticle 未赋值！请在 Inspector 中拖入粒子系统。");
            return;
        }

        // 确保粒子系统所在 GameObject 是激活状态
        if (!slashParticle.gameObject.activeSelf)
        {
            slashParticle.gameObject.SetActive(true);
            Debug.Log("[PlaySlashEffect] 粒子系统 GameObject 原本未激活，已强制激活。");
        }

        // 防御：关闭 PlayOnAwake，避免自动播放干扰手动控制
        if (slashParticle.main.playOnAwake)
        {
            var main = slashParticle.main;
            main.playOnAwake = false;
            Debug.Log("[PlaySlashEffect] playOnAwake 原本为 true，已强制设为 false。");
        }

        // 使用 Clear() + time=0 + Play() 替代 Stop() + Play()
        // 原因：非循环粒子系统在自然停止后，Stop() 可能导致 Play() 无法重新触发 Burst
        slashParticle.Clear();
        slashParticle.time = 0;
        slashParticle.Play();

    }

    public void PlayTrailEffect()
    {
        if (trailParticle == null)
        {
            Debug.LogWarning("[PlayTrailEffect] trailParticle 未赋值！请在 Inspector 中拖入粒子系统。");
            return;
        }

        // 确保粒子系统所在 GameObject 是激活状态
        if (!trailParticle.gameObject.activeSelf)
        {
            trailParticle.gameObject.SetActive(true);
            Debug.Log("[PlayTrailEffect] 粒子系统 GameObject 原本未激活，已强制激活。");
        }

        // 防御：关闭 PlayOnAwake，避免自动播放干扰手动控制
        if (trailParticle.main.playOnAwake)
        {
            var main = trailParticle.main;
            main.playOnAwake = false;
            Debug.Log("[PlayTrailEffect] playOnAwake 原本为 true，已强制设为 false。");
        }

        // 使用 Clear() + time=0 + Play() 替代 Stop() + Play()
        // 原因：非循环粒子系统在自然停止后，Stop() 可能导致 Play() 无法重新触发 Burst
        trailParticle.Clear();
        trailParticle.time = 0;
        trailParticle.Play();

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
        yield return new WaitForSeconds(0.3f);
        animator.speed = 1f;
    }

}
