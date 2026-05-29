using UnityEngine;

public class MouseManager : MonoBehaviour
{
    [Header("旋转设置")]
    public float mouseSensitivity = 100f;   // 鼠标灵敏度
    public float upDownRange = 80f;         // 上下旋转角度限制（度）

    [Header("缩放设置")]
    public float scrollSensitivity = 5f;    // 滚轮灵敏度
    public float minZoom = 2f;              // 最小缩放距离
    public float maxZoom = 20f;             // 最大缩放距离

    private float currentXRotation = 0f;    // 当前上下角度
    private Camera mainCamera;
    float _targetRot;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("请将此脚本挂载到带有Camera组件的物体上");
        }

        // 锁定光标到游戏窗口（可选，按Esc可解锁）
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {

        // 按Esc键释放/锁定鼠标
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ?
                               CursorLockMode.None : CursorLockMode.Locked;
        }

        // 仅在鼠标锁定时才控制视角
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMouseRotation();
            HandleMouseZoom();
        }
    }

    void HandleMouseRotation()
    {
        // 获取鼠标移动增量
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 左右旋转（绕世界Y轴）
        transform.Rotate(Vector3.up, mouseX);

        // 上下旋转（绕局部X轴）并限制范围
        currentXRotation -= mouseY;
        currentXRotation = Mathf.Clamp(currentXRotation, -upDownRange, upDownRange);
        transform.localRotation = Quaternion.Euler(currentXRotation, transform.eulerAngles.y, 0f);
    }

    void HandleMouseZoom()
    {
        // 获取滚轮输入
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // 对于透视相机：移动位置
            if (mainCamera.orthographic == false)
            {
                Vector3 direction = transform.forward;
                float zoomAmount = scroll * scrollSensitivity;
                Vector3 newPosition = transform.position + direction * zoomAmount;

                // 限制距离（假设摄像机看向某个点，这里简化处理）
                // 如果需要围绕物体缩放，需要计算与目标点的距离限制
                float distance = Vector3.Distance(newPosition, Vector3.zero);
                if (distance >= minZoom && distance <= maxZoom)
                {
                    transform.position = newPosition;
                }
            }
            else // 正交相机：改变size
            {
                mainCamera.orthographicSize -= scroll * scrollSensitivity;
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
            }
        }
    }
}