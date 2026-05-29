using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CubeManager : MonoBehaviour
{
    [Header("预制体与标记点")]
    public GameObject cubePrefab;      // Cube预制体
    public Transform spawnMarker;      // 空物体标记点（第一个Cube生成位置）

    [Header("参数设置")]
    public float scaleDuration = 3f;   // 每次缩放耗时
    //public float waitTime = 0.03f;        // 恢复后等待时间
    public float scaleMultiplier = 1.3f; // 缩放倍数
    public float offset = 0.3f;   // 脉冲间隔时间

    private List<GameObject> allCubes = new List<GameObject>(); // 存储所有Cube
    private Vector3[] originalScales;   // 存储每个Cube的原始大小

    void Start()
    {
        cubePrefab = Resources.Load<GameObject>("Prefab/Cube");
        if (cubePrefab == null)
        {
            Debug.LogError("请指定Cube预制体");
            return;
        }
        if (spawnMarker == null)
        {
            Debug.LogError("请指定空物体标记点");
            return;
        }

        GenerateCubes();
        StartCoroutine(PulseAllCubes());
    }

    /// <summary>
    /// 生成9个Cube：中心1个 + 8个顶点
    /// </summary>
    void GenerateCubes()
    {
        // 1. 先生成中心Cube
        GameObject centerCube = Instantiate(cubePrefab, spawnMarker.position, Quaternion.identity);
        allCubes.Add(centerCube);

        // 2. 获取中心Cube的实际大小（基于它当前的缩放）
        float cubeSize = centerCube.transform.localScale.x;
        float halfSize = cubeSize / 2f;

        // 3. 计算8个顶点位置（基于中心Cube的边界）
        Vector3[] vertexOffsets = new Vector3[]
        {
        new Vector3(-(halfSize - offset), -(halfSize - offset), -(halfSize - offset)), // 左下后
        new Vector3( halfSize - offset, -(halfSize - offset), -(halfSize - offset)), // 右下后
        new Vector3(-(halfSize - offset),  halfSize - offset, -(halfSize - offset)), // 左上前
        new Vector3( halfSize - offset,  halfSize - offset, -(halfSize - offset)), // 右上前
        new Vector3(-(halfSize - offset), -(halfSize - offset),  halfSize - offset), // 左下前
        new Vector3( halfSize - offset, -(halfSize - offset),  halfSize - offset), // 右下前
        new Vector3(-(halfSize - offset),  halfSize - offset,  halfSize - offset), // 左上前
        new Vector3( halfSize - offset,  halfSize - offset,  halfSize - offset)  // 右上前
        };

        // 4. 在顶点位置生成外围Cube
        foreach (Vector3 offset in vertexOffsets)
        {
            Vector3 vertexPos = spawnMarker.position + offset;
            GameObject vertexCube = Instantiate(cubePrefab, vertexPos, Quaternion.identity);
            // 关键：外围Cube也要设置成和中心Cube同样的大小
            vertexCube.transform.localScale = centerCube.transform.localScale * 0.6f;
            allCubes.Add(vertexCube);
        }

        // 5. 存储原始缩放
        originalScales = new Vector3[allCubes.Count];
        for (int i = 0; i < allCubes.Count; i++)
        {
            originalScales[i] = allCubes[i].transform.localScale;
        }
    }

    /// <summary>
    /// 控制所有Cube同步缩放脉冲
    /// </summary>
    IEnumerator PulseAllCubes()
    {
        while (true)
        {
            // 所有Cube同时变大
            yield return ScaleAllCubesOverTime(originalScales, GetTargetScales(), scaleDuration);

            // 所有Cube同时恢复
            yield return ScaleAllCubesOverTime(GetCurrentScales(), originalScales, scaleDuration);

            // 等待3秒
            yield return new WaitForSeconds(0.03f);
        }
    }

    /// <summary>
    /// 在指定时间内将所有Cube从起始大小平滑变化到目标大小
    /// </summary>
    IEnumerator ScaleAllCubesOverTime(Vector3[] fromScales, Vector3[] toScales, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time; // 线性插值系数

            // 同时更新所有Cube的缩放
            for (int i = 0; i < allCubes.Count; i++)
            {
                if (allCubes[i] != null)
                {
                    allCubes[i].transform.localScale = Vector3.Lerp(fromScales[i], toScales[i], t);
                }
            }
            yield return null;
        }

        // 确保所有Cube精确到达目标大小
        for (int i = 0; i < allCubes.Count; i++)
        {
            if (allCubes[i] != null)
            {
                allCubes[i].transform.localScale = toScales[i];
            }
        }
    }

    /// <summary>
    /// 获取所有Cube当前缩放值
    /// </summary>
    Vector3[] GetCurrentScales()
    {
        Vector3[] currentScales = new Vector3[allCubes.Count];
        for (int i = 0; i < allCubes.Count; i++)
        {
            if (allCubes[i] != null)
                currentScales[i] = allCubes[i].transform.localScale;
        }
        return currentScales;
    }

    /// <summary>
    /// 计算所有Cube的目标缩放值（原始大小 × 倍数）
    /// </summary>
    Vector3[] GetTargetScales()
    {
        Vector3[] targetScales = new Vector3[originalScales.Length];
        for (int i = 0; i < originalScales.Length; i++)
        {
            targetScales[i] = originalScales[i] * scaleMultiplier;
        }
        return targetScales;
    }
}