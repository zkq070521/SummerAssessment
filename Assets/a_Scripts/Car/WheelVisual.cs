using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelVisual : MonoBehaviour
{
    public WheelCollider wheelCollider;
    public Transform wheelMesh;

    public Vector3 position;
    private Quaternion rotation;

    void Update()
    {
        // 将车轮模型移动到对应位置
        wheelMesh.position = wheelCollider.transform.position;
        wheelMesh.rotation = wheelCollider.transform.rotation;
    }
}