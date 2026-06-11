using UnityEngine;

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

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _transform = transform;
        _input = new InputController();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        _input.Player.Enable();
    }

    void OnDisable()
    {
        _input.Player.Disable();
    }

    void Update()
    {
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
}
