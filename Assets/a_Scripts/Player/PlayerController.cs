using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerSimple : MonoBehaviour
{
    private CharacterController _controller;
    private Camera _mainCamera;  // 直接引用Camera

    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("旋转设置")]
    public float rotationSmoothTime = 0.12f;
    private float _currentRotationVelocity;

    private Vector2 _moveInput;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        // 直接获取主摄像机
        _mainCamera = Camera.main;

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
        // 检查摄像机是否存在
        if (_mainCamera == null) return;

        // 获取键盘WASD输入
        _moveInput.x = Input.GetAxis("Horizontal");  // A/D: -1/1
        _moveInput.y = Input.GetAxis("Vertical");    // W/S: -1/1

        // 获取相机的变换组件
        Transform cameraTransform = _mainCamera.transform;

        // 获取相机的前方向和右方向，忽略上下倾斜（只取水平方向）
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 根据输入计算移动方向（相对于相机）
        // W/S: 相机前/后方向，A/D: 相机左/右方向
        Vector3 moveDir = (cameraForward * _moveInput.y + cameraRight * _moveInput.x).normalized;

        if (_moveInput != Vector2.zero)
        {
            // 角色平滑转向移动方向
            float targetRot = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float currentRot = transform.eulerAngles.y;
            float newRot = Mathf.SmoothDampAngle(currentRot, targetRot, ref _currentRotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, newRot, 0f);

            // 移动
            _controller.Move(moveDir * (moveSpeed * Time.deltaTime));
        }
        else
        {
            _currentRotationVelocity = 0f;
        }
    }
}