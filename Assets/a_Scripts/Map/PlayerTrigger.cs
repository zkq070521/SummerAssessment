using UnityEngine;

public class PlayerTrigger : MonoBehaviour
{
    public TrackGenerator trackGenerator;

    private int segmentsPassed = 0;

    void OnTriggerEnter(Collider other)
    {
        // 如果碰到触发区域（我们将在每个路段末尾添加触发器）
        if (other.CompareTag("Middle"))
        {

            trackGenerator.GenerateNextSegment();

        }
    }
}