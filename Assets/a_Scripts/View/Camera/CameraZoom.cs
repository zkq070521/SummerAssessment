using UnityEngine;
using Cinemachine;

public class CameraZoomWithCinemachine : MonoBehaviour
{
    public CinemachineFreeLook freeLook;
    public float minDistance = 0.5f;
    public float maxDistance = 2f;
    public float scrollSensitivity = 1f;

    private float[] originalRadii;

    void Start()
    {
        if (freeLook != null)
        {
            originalRadii = new float[3];
            for (int i = 0; i < 3; i++)
            {
                originalRadii[i] = freeLook.m_Orbits[i].m_Radius;
                Debug.Log($"Rig {i} ({CinemachineFreeLook.RigNames[i]}) 初始半径: {originalRadii[i]:F2}");
            }
        }
    }

    void Update()
    {
        if (freeLook == null || originalRadii == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        // 以 Middle Rig 的当前半径为基准计算新距离
        float currentRadius = freeLook.m_Orbits[1].m_Radius;
        float newRadius = Mathf.Clamp(
            currentRadius - scroll * scrollSensitivity,
            minDistance,
            maxDistance
        );

        // 按初始比例同步缩放所有 3 个 Rig，保持轨道曲线形状
        float scale = originalRadii[1] > 0.001f
            ? newRadius / originalRadii[1]
            : 1f;

        for (int i = 0; i < 3; i++)
        {
            freeLook.m_Orbits[i].m_Radius = originalRadii[i] * scale;
        }
    }
}
