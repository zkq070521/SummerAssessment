using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Conditional")]
[TaskDescription("检测玩家是否在视野范围内（360度）")]
public class CanSeePlayer : Conditional
{
    public SharedGameObject targetPlayer;  // 输出找到的玩家
    public float viewDistance = 10f;       // 视野距离
    public LayerMask targetLayer;          // 目标层级（Player）
    public LayerMask obstacleLayer;        // 障碍物层级
    public bool clearOnFailure = true;
    public override TaskStatus OnUpdate()
    {
        // 1. 搜索范围内的玩家（球形检测）
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance, targetLayer);

        if (hitColliders.Length > 0)
        {
            // 遍历所有检测到的玩家
            foreach (Collider collider in hitColliders)
            {
                Transform playerTransform = collider.transform;
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

                // 2. 射线检测遮挡（没有角度限制）
                RaycastHit hit;
                if (!Physics.Raycast(transform.position, directionToPlayer, out hit, viewDistance, obstacleLayer))
                {
                    // 看见玩家了（没有被遮挡）
                    targetPlayer.Value = playerTransform.gameObject;
                    return TaskStatus.Success;
                }
            }
        }

        // 只在允许的情况下清空变量
        if (clearOnFailure)
        {
            targetPlayer.Value = null;
        }

        return TaskStatus.Failure;
    }

    // 可视化调试：在Scene视图中显示视野范围
    public override void OnDrawGizmos()
    {
        if (transform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}