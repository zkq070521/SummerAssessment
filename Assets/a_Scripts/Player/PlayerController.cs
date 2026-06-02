using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerSimple : MonoBehaviour
{
    private CharacterController _controller;
    private CameraController _cameraController;

    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("旋转设置")]
    public float rotationSmoothTime = 0.12f;
    private float _currentRotationVelocity;

    private Vector2 _moveInput;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _cameraController = FindObjectOfType<CameraController>();
    }

    void Update()
    {
        if (_cameraController == null) return;

        // 获取键盘WASD输入
        _moveInput.x = Input.GetAxis("Horizontal");  // A/D: -1/1
        _moveInput.y = Input.GetAxis("Vertical");    // W/S: -1/1

        // 获取相机的水平旋转角度
        float cameraYaw = _cameraController.CameraYaw;

        // 计算移动方向
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        Vector3 moveDir = Quaternion.Euler(0f, cameraYaw, 0f) * inputDir;

        if (_moveInput != Vector2.zero)
        {
            // 角色面向移动方向
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