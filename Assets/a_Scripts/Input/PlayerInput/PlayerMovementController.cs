using UnityEngine;
using UnityEngine.InputSystem;

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
    public float weaponHideDelay = 1.8f;        // 武器显示后隐藏延迟
    public ParticleSystem slashParticle;  // 拖入你的WeaponTrail粒子系统

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



        // 2. 播放攻击动画
        animator.SetTrigger(AttackHash);

        // 1. 前冲：朝角色面朝方向快速移动
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

        // 3. 显示武器
        if (weaponObject != null)
        {
            weaponObject.SetActive(true);
            PlaySlashEffect();  // 播放刀光效果
        }
        _isAttacking = false;

        // 4. 延迟后隐藏武器
        yield return new WaitForSeconds(weaponHideDelay);

        if (weaponObject != null)
            weaponObject.SetActive(false);
    }

    // 播放刀光
    public void PlaySlashEffect()
    {
        if (slashParticle == null) return;

        slashParticle.Stop();                     // 先停止，重置状态
        slashParticle.Clear();                    // 清除残留粒子
        slashParticle.Play();                     // 开始播放

        // 可选：自动停止（防止一直循环）
        // Invoke(nameof(StopSlashEffect), effectDuration);
    }

    #endregion
}
