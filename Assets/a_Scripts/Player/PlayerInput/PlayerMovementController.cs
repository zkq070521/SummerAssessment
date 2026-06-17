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

    // 组件
    private CharacterController _controller;
    private Transform _transform;
    public InputController _input;

    // 状态
    private Vector2 _moveInput;
    private bool _isSprinting;
    private Vector3 _smoothVelocity;
    private float _rotationVelocity;
    private float _verticalVelocity;

    // 输入封锁
    private bool _inputBlocked;

    // 攻击
    private InputAction _attackAction;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackContinueHash = Animator.StringToHash("AttackContinue");
    private int comboIndex = 1;      // 当前是第几段
    private bool canCombo = false;   // 是否允许按下一段
    private float comboWindow = 0.5f; // 窗口时间，在动画的后半段开启

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

    void OnEnable()
    {
        _input.Player.Enable();
        _attackAction?.Enable();
        InputService.OnInputEnabledChanged += OnInputEnabledChanged;
    }

    void OnDisable()
    {
        _input.Player.Disable();
        _attackAction?.Disable();
        InputService.OnInputEnabledChanged -= OnInputEnabledChanged;
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
        if (_inputBlocked) return;

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

    #region 攻击

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (_inputBlocked) return;
        if (animator == null) return;
        // 如果当前没有在攻击，则从第一段开始
        if (!IsAttacking())
        {
            StartAttack();
        }
        // 如果正在攻击，且处于允许连击的时间窗口内
        else if (canCombo)
        {
            // 立刻增加连击数，并强制触发下一段
            comboIndex++;
            if (comboIndex > 2) comboIndex = 1; // 三段循环

            // 设置参数，触发过渡（打断）
            animator.SetInteger("ComboIndex", comboIndex);
            animator.SetTrigger(AttackContinueHash);
            canCombo = false; // 重置窗口，防止一次连按触发多次
        }

        // 显示武器物体
        if (weaponObject != null)
            weaponObject.SetActive(true);

    }

    void StartAttack()
    {
        comboIndex = 1;
        animator.SetInteger("ComboIndex", comboIndex);
        animator.SetTrigger(AttackHash);
        // 开启接受输入，但在动画的特定帧才允许打断（见下面的事件）
        canCombo = false;
    }

    bool IsAttacking()
    {
        // 检查当前状态是否是攻击状态（通过Tag或名称判断）
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    // 这个函数由动画事件（Animation Event）调用
    public void EnableComboWindow()
    {
        canCombo = true;
    }

    // 这个函数由动画事件调用，在动画结束时关闭窗口并重置状态
    public void DisableComboWindow()
    {
        canCombo = false;
        // 如果不小心没触发连击，把Index重置回1，防止下次卡住
        comboIndex = 1;
        animator.SetInteger("ComboIndex", comboIndex);
    }

    #endregion
}
