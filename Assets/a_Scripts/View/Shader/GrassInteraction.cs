using UnityEngine;

public class GrassInteraction : MonoBehaviour
{
    [Header("麦田材质")]
    public Material grassMaterial; // 拖入你创建的麦田材质球

    [Header("交互角色")]
    public Transform player; // 拖入你的角色

    [Header("调试")]
    public bool showDebugGizmo = true;

    private int propertyID_Position;
    private int propertyID_Enabled;

    void Start()
    {
        // 获取Shader属性ID（比字符串更快）
        propertyID_Position = Shader.PropertyToID("_InteractionPosition");
        propertyID_Enabled = Shader.PropertyToID("_InteractionEnabled");
    }

    void Update()
    {
        if (grassMaterial == null || player == null) return;

        // 把角色位置传给Shader
        grassMaterial.SetVector(propertyID_Position, player.position);
        grassMaterial.SetFloat(propertyID_Enabled, 1.0f);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmo || player == null) return;

        // 在Scene视图中显示交互半径（方便调试）
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(player.position, 1.5f);
    }
}