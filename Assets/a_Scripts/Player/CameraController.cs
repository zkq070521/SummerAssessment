using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("距离控制")]
    public float distance = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    public float scrollSensitivity = 3f;
    public float zoomSmoothTime = 0.15f;

    [Header("旋转控制")]
    public float mouseSensitivity = 2f;
    public float pitchMin = -40f;  // 上下看的最小角度（向下）
    public float pitchMax = 70f;   // 上下看的最大角度（向上）

    [Header("平滑控制")]
    public float positionSmoothTime = 0.08f;

    [Header("碰撞检测")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.25f;
    public bool enableCollision = true;

    private float _yaw;      // 水平旋转（左右）
    private float _pitch;    // 垂直旋转（上下）
    private float _targetDistance;
    private float _distanceVelocity;
    private Vector3 _positionVelocity;
    private float _currentDistance;

    private bool _isCursorLocked;
    private Vector2 _lookInput;

    // 公共属性，供PlayerController使用
    public float CameraYaw => _yaw;

    void Start()
    {
        _targetDistance = distance;
        _currentDistance = distance;

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (target != null)
        {
            Vector3 dir = transform.position - GetTargetPosition();
            _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            _pitch = -Mathf.Asin(dir.y / Mathf.Max(dir.magnitude, 0.01f)) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
        }

        LockCursor();
    }

    void Update()
    {
        HandleCursorInput();
        if (!_isCursorLocked) return;

        HandleRotation();
        HandleZoom();
    }

    void LateUpdate()
    {
        if (target == null) return;
        UpdateCameraTransform();
    }

    private void HandleCursorInput()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isCursorLocked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _isCursorLocked = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        _isCursorLocked = false;
    }

    private void HandleRotation()
    {
        // 获取鼠标移动增量
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // 左右移动鼠标 → 控制水平旋转（yaw）
        _yaw += mouseDelta.x * mouseSensitivity;

        // 上下移动鼠标 → 控制垂直旋转（pitch）
        _pitch -= mouseDelta.y * mouseSensitivity;

        // 限制上下看的角度范围
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
    }

    private void HandleZoom()
    {
        // 滚轮控制缩放
        float scroll = Mouse.current.scroll.y.ReadValue() / 120f;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _targetDistance -= scroll * scrollSensitivity;
            _targetDistance = Mathf.Clamp(_targetDistance, minDistance, maxDistance);
        }

        _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, zoomSmoothTime);
    }

    private void UpdateCameraTransform()
    {
        Vector3 targetPos = GetTargetPosition();

        // 根据yaw和pitch计算相机旋转
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPos = targetPos - (rotation * Vector3.forward) * _currentDistance;

        if (enableCollision)
        {
            Vector3 dir = (desiredPos - targetPos).normalized;
            float checkDist = Vector3.Distance(targetPos, desiredPos);
            if (Physics.SphereCast(targetPos, collisionRadius, dir, out RaycastHit hit, checkDist, collisionMask))
            {
                desiredPos = targetPos + dir * Mathf.Max(hit.distance - collisionRadius, 0.1f);
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _positionVelocity, positionSmoothTime);
        transform.rotation = Quaternion.LookRotation((targetPos - transform.position).normalized);
    }

    private Vector3 GetTargetPosition()
    {
        return target.position + targetOffset;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetTargetPosition(), collisionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(GetTargetPosition(), transform.position);
    }
}