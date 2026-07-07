using UnityEngine;

public class WheatFieldGenerator : MonoBehaviour
{
    [Header("麦子预制体")]
    public GameObject wheatPrefab;   // 👈 把你做好的麦子预制体拖进来

    [Header("麦田大小")]
    public int rows = 30;            // 行数
    public int cols = 30;            // 列数
    public float spacing = 0.4f;     // 间距（值越小越密）

    [Header("随机变化")]
    public float heightMin = 0.7f;   // 最矮高度
    public float heightMax = 1.3f;   // 最高高度
    public float randomRotate = 360f; // 随机旋转角度（0=不旋转，360=完全随机）

    void Start()
    {
        GenerateField();
    }

    void GenerateField()
    {
        if (wheatPrefab == null)
        {
            Debug.LogError("❌ 请把麦子预制体拖到 Wheat Prefab 槽里！");
            return;
        }

        // 计算偏移，让麦田居中
        float offsetX = (rows - 1) * spacing * 0.5f;
        float offsetZ = (cols - 1) * spacing * 0.5f;

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < cols; z++)
            {
                // 计算位置
                Vector3 pos = new Vector3(x * spacing - offsetX, 0, z * spacing - offsetZ);

                // 生成麦子
                GameObject wheat = Instantiate(wheatPrefab, pos, Quaternion.identity, transform);

                // 随机高度（让麦田有层次感）
                float height = Random.Range(heightMin, heightMax);
                wheat.transform.localScale = new Vector3(1, height, 1);

                // 随机旋转（让麦子朝向不同方向，更自然）
                // 使用乘法叠加预制体的基础旋转，避免覆盖掉让麦子竖起来的 X 轴旋转
                float rotY = Random.Range(0, randomRotate);
                wheat.transform.rotation = wheatPrefab.transform.rotation * Quaternion.Euler(0, rotY, 0);
            }
        }

        Debug.Log($"✅ 麦田生成完成！共 {rows * cols} 棵麦子。");
    }
}