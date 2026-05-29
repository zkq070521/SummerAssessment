using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private GameObject _mainCamera;
    private CharacterController _controller;
    public float moveSpeed = 5f;

    [Header("旋转设置")]
    public float rotationSmoothTime = 0.1f; // 旋转平滑时间
    private float _currentRotationVelocity; // 用于平滑旋转

    [Header("输入配置")]
    private InputController inputController;
    private InputActionMap playerMap;
    private InputAction moveAction;
    private Vector2 _input;

    void Awake()
    {
        inputController = new InputController();
        playerMap = inputController.Player;
        moveAction = playerMap.FindAction("PlayerMove");

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;

        playerMap.Enable();
    }

    private void Start()
    {
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_input != Vector2.zero)
        {
            // 计算移动方向（相对于相机）
            Vector3 inputDir = new Vector3(_input.x, 0f, _input.y).normalized;

            // 计算目标旋转角度
            float targetRot = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;

            // 平滑旋转
            float currentRot = transform.eulerAngles.y;
            float newRot = Mathf.SmoothDampAngle(currentRot, targetRot, ref _currentRotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, newRot, 0.0f);

            // 移动
            Vector3 targetDir = transform.forward;
            _controller.Move(targetDir * (moveSpeed * Time.deltaTime));
        }
        // 没有输入时：不进行任何旋转，直接重置速度变量
        else
        {
            // 重置平滑旋转的速度变量，防止惯性
            _currentRotationVelocity = 0f;
            // 不修改 rotation，保持当前朝向
        }
    }
    void OnEnable()
    {
        if (playerMap != null)
            playerMap.Enable();
    }

    void OnDisable()
    {
        if (playerMap != null)
            playerMap.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _input = Vector2.zero;
    }
}