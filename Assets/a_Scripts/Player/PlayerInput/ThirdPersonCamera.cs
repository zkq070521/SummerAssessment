using UnityEngine;
using UnityEngine.InputSystem;


public class ThirdPersonCamera : MonoBehaviour
{
    private GameObject _mainCamera;
    [Header("鼠标灵敏度")]
    [Tooltip("鼠标灵敏度，值越小转动越慢")]
    public float mouseSensitivity = 0.5f;
    [Header("Cinemachine")]
    [Tooltip("要跟随的目标")]
    public GameObject CameraTarget;

    [Tooltip("上移的最大角度")]
    public float TopClamp = 80.0f;

    [Tooltip("下移的最大角度")]
    public float BottomClamp = -50.0f;

    private const float _threshold = 0.01f;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;



    [Header("输入配置")]
    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap playerMap;
    private InputAction lookAction;

    private Vector2 _look;

    void Awake()
    {
        playerMap = inputActions.FindActionMap("Player");

        // 找到具体的动作
        lookAction = playerMap.FindAction("PlayerLook");


        lookAction.performed += OnLook;

    }
    void OnEnable()
    {
        // 启用输入
        playerMap.Enable();
    }
    void OnDisable()
    {
        // 禁用输入
        playerMap.Disable();
    }

    private void Start()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _cinemachineTargetYaw = CameraTarget.transform.rotation.eulerAngles.y;
    }

    private void Update()
    {
        if (_look.sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += _look.x * mouseSensitivity;
            _cinemachineTargetPitch += _look.y * mouseSensitivity;

            // 用完就重置，避免持续累加
            _look = Vector2.zero;

        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        Vector3 currentRotation = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);

    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        _look = context.ReadValue<Vector2>();
    }




}
