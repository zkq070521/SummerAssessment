using UnityEngine;
using System.Collections.Generic;

public class DrillGenerator : MonoBehaviour
{
    [Header("位置标记")]
    public Transform drillTip;              // 钻头尖端位置（场景中的空物体）

    [Header("螺旋参数")]
    public float startRadius = 0.2f;        // 起始半径（尖端）
    public float endRadius = 2f;            // 结束半径（尾部）
    public float height = 5f;               // 钻头总高度
    public int totalCubes = 50;             // Cube总数量
    public float turns = 3f;                // 旋转圈数

    [Header("Cube大小参数")]
    public float minCubeSize = 0.3f;        // 最小Cube大小（尖端）
    public float maxCubeSize = 0.8f;        // 最大Cube大小（尾部）

    [Header("旋转速度")]
    public float rotationSpeed = 25f;       // 整体旋转速度（度/秒）

    private List<GameObject> drillCubes = new List<GameObject>();
    private GameObject cubePrefab;
    private GameObject drillContainer;       // 用来存放所有Cube的容器

    void Start()
    {
        // 检查是否指定了尖端位置
        if (drillTip == null)
        {
            Debug.LogError("请将场景中的空物体拖拽到 drillTip 字段！");
            return;
        }

        // 加载预制体
        cubePrefab = Resources.Load<GameObject>("Prefab/cube");
        if (cubePrefab == null)
        {
            Debug.LogError("未找到预制体！路径：Resources/Prefab/cube");
            return;
        }

        GenerateDrill();
    }

    void Update()
    {
        // 让整个钻头旋转
        if (drillContainer != null)
        {
            drillContainer.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    void GenerateDrill()
    {
        // 创建一个容器来存放所有Cube，方便管理
        drillContainer = new GameObject("DrillContainer");
        drillContainer.transform.position = drillTip.position;

        for (int i = 0; i < totalCubes; i++)
        {
            // 计算进度 t (0=尖端, 1=尾部)
            float t = (float)i / (totalCubes - 1);

            // 1. 计算半径（从尖端到尾部逐渐变大）
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            // 2. 计算高度位置（从尖端向上延伸）
            float y = t * height;

            // 3. 计算旋转角度（螺旋）
            float angle = t * turns * 360f;
            float rad = angle * Mathf.Deg2Rad;

            // 4. 计算X和Z坐标（局部坐标）
            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;

            // 5. 计算Cube大小（尖端小，尾部大）
            float cubeSize = Mathf.Lerp(minCubeSize, maxCubeSize, t);

            // 6. 生成Cube（位置相对于drillContainer）
            Vector3 localPos = new Vector3(x, y, z);
            GameObject cube = Instantiate(cubePrefab, drillContainer.transform);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = Vector3.one * cubeSize;

            // 7. 让Cube朝向螺旋方向（可选）
            MakeCubeFaceDirection(cube, radius, angle, y);

            drillCubes.Add(cube);
        }

        Debug.Log($"在空物体 {drillTip.name} 位置生成了 {drillCubes.Count} 个Cube组成的螺旋钻头");
    }

    /// <summary>
    /// 让Cube朝向螺旋线的切线方向（可选功能）
    /// </summary>
    void MakeCubeFaceDirection(GameObject cube, float radius, float angle, float y)
    {
        // 简化版：让Cube朝向外侧
        float rad = angle * Mathf.Deg2Rad;
        Vector3 outwardDir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad));

        cube.transform.rotation = Quaternion.LookRotation(outwardDir, Vector3.up);
        cube.transform.Rotate(90, 0, 0); // 根据你的Cube模型调整
    }

    /// <summary>
    /// 在Inspector中可视化螺旋线（调试用）
    /// </summary>
    void OnDrawGizmos()
    {
        if (drillTip == null) return;

        Gizmos.color = Color.red;
        float step = 0.02f;
        for (float t = 0; t <= 1; t += step)
        {
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float y = t * height;
            float angle = t * turns * 360f;
            float rad = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(rad) * radius;
            float z = Mathf.Sin(rad) * radius;

            Vector3 pos = drillTip.position + new Vector3(x, y, z);

            float nextT = Mathf.Min(t + step, 1);
            float nextRadius = Mathf.Lerp(startRadius, endRadius, nextT);
            float nextY = nextT * height;
            float nextAngle = nextT * turns * 360f;
            float nextRad = nextAngle * Mathf.Deg2Rad;
            float nextX = Mathf.Cos(nextRad) * nextRadius;
            float nextZ = Mathf.Sin(nextRad) * nextRadius;
            Vector3 nextPos = drillTip.position + new Vector3(nextX, nextY, nextZ);

            Gizmos.DrawLine(pos, nextPos);
        }

        // 在尖端位置绘制一个黄色球体标记
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(drillTip.position, 0.1f);
    }
}