using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BreakScreen : MonoBehaviour
{
    public GameObject explosionPosition;

    private void Start()  // 改为 Start，比 Awake 晚一帧
    {
        StartCoroutine(ExplodeAfterDelay(0.1f)); // 延迟0.1秒
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("爆炸开始！");
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                // 增加向上修正力，让碎片明显飞起
                childRigidbody.AddExplosionForce(1000f, explosionPosition.transform.position, 5f, 3f);

                // 可选：让碎片脱离父物体
                child.parent = null;
            }
        }
    }
}