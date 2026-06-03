using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerSimple : MonoBehaviour
{
    private CharacterController _controller;
    private Camera _mainCamera;  // 直接引用Camera

    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private bool _isRunning;
    private float _horizontalSpeed;

    [Header("旋转设置")]
    public float rotationSmoothTime = 0.12f;
    private float _currentRotationVelocity;

    private Vector2 _moveInput;

    [Header("动画设置")]
    private Animator _animator;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        // 直接获取主摄像机
        _mainCamera = Camera.main;
        _isRunning = false;

        // 如果找不到主摄像机，尝试用标签找
        if (_mainCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null)
                _mainCamera = camObj.GetComponent<Camera>();
        }
    }

    void Update()
    {
        _moveInput.x = Input.GetAxis("Horizontal");  // A/D: -1/1
        _moveInput.y = Input.GetAxis("Vertical");    // W/S: -1/1

        // 切换跑步状态
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            _isRunning = !_isRunning;
        }


        if (_mainCamera != null)
        {
            Transform cameraTransform = _mainCamera.transform;
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDir = (cameraForward * _moveInput.y + cameraRight * _moveInput.x).normalized;

            if (_moveInput != Vector2.zero)
            {
                // 角色平滑转向移动方向
                float targetRot = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                float currentRot = transform.eulerAngles.y;
                float newRot = Mathf.SmoothDampAngle(currentRot, targetRot, ref _currentRotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, newRot, 0f);

                // 移动
                _controller.Move(moveDir * (_isRunning ? runSpeed : moveSpeed) * Time.deltaTime);
            }
            else
            {
                _currentRotationVelocity = 0f;
            }
        }


        Vector3 currentVelocity = _controller.velocity;
        float horizontalSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
        _horizontalSpeed = horizontalSpeed;


        // 如果没有移动输入，且实际速度很小，强制归零
        if (_moveInput == Vector2.zero)
        {
            _horizontalSpeed = 0f;
        }


        SetAnimation();


    }

    public void SetAnimation()
    {
        if (_animator != null)
        {
            _animator.SetFloat("Speed", _horizontalSpeed);
            _animator.SetBool("isRunning", _isRunning);

        }
    }
}

