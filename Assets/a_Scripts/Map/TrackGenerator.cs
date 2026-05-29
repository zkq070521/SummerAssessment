using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    // 存放所有地形预制体的数组
    public GameObject[] roadPrefabs;

    // 初始要生成多少段路
    public int initialTrackLength = 1;

    // 玩家身后的路段保留多少个（超出就删除）
    public int maxTrackSegments = 1;

    // 存储当前所有已经生成的路段
    private List<GameObject> activeSegments = new List<GameObject>();

    // 下一个路段的生成位置
    private Vector3 nextSpawnPoint;

    void Start()
    {
        // 从原点开始生成
        nextSpawnPoint = Vector3.zero;

        // 生成初始赛道
        for (int i = 0; i < initialTrackLength; i++)
        {
            GenerateNextSegment();
        }
    }

    // 生成下一个路段
    public void GenerateNextSegment()
    {
        // 1. 随机选择一个预制体
        int randomIndex = Random.Range(0, roadPrefabs.Length);
        GameObject selectedPrefab = roadPrefabs[randomIndex];

        // 2. 实例化（生成）这个路段
        GameObject newSegment = Instantiate(selectedPrefab, nextSpawnPoint, Quaternion.identity);

        // 3. 将新路段加入列表
        activeSegments.Add(newSegment);

        // 4. 更新下一个生成点的位置（当前路段长度10米，Z轴方向）
        nextSpawnPoint += new Vector3(0, 0, 40);

        // 5. 如果路段数量超过限制，删除最早的路段
        if (activeSegments.Count > maxTrackSegments)
        {
            RemoveOldestSegment();
        }
    }

    // 删除最旧的路段
    void RemoveOldestSegment()
    {
        GameObject oldestSegment = activeSegments[0];
        activeSegments.RemoveAt(0);
        Destroy(oldestSegment);
    }

    // 这个方法供外部调用（比如玩家的车触发时）
    public void OnPlayerPassedSegment()
    {
        GenerateNextSegment();
    }
}