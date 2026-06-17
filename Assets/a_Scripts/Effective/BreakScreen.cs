using System.Collections;
using UnityEngine;

public class BreakScreen : MonoBehaviour
{
    public GameObject explosionPosition;

    void OnEnable()
    {
        GameEvents.OnHitEnemy += OnHitEnemyHandler;
    }

    void OnDisable()
    {
        GameEvents.OnHitEnemy -= OnHitEnemyHandler;
    }

    private void OnHitEnemyHandler(GameObject enemy, Vector3 hitPoint)
    {
        // 开始碎裂
        StartCoroutine(ExplodeAfterDelay(0.1f));
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("玻璃碎裂！");
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                childRigidbody.AddExplosionForce(1000f, explosionPosition.transform.position, 5f, 3f);
                child.parent = null; // 脱离父物体
            }
        }
    }
}