using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Conditional")]
[TaskDescription("检测玩家是否在视野范围内")]
public class CanSeePlayer : Conditional
{
    public SharedGameObject targetPlayer;  // 输出找到的玩家
    public float viewDistance = 10f;       // 视野距离
    public float viewAngle = 60f;          // 视野角度
    public LayerMask targetLayer;          // 目标层级（Player）
    public LayerMask obstacleLayer;        // 障碍物层级

    private Transform playerTransform;

    public override TaskStatus OnUpdate()
    {
        // 1. 搜索范围内的玩家
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, viewDistance, targetLayer);

        if (hitColliders.Length > 0)
        {
            playerTransform = hitColliders[0].transform;
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            // 2. 角度判断
            if (angleToPlayer < viewAngle / 2)
            {
                // 3. 射线检测遮挡
                RaycastHit hit;
                if (!Physics.Raycast(transform.position, directionToPlayer, out hit, viewDistance, obstacleLayer))
                {
                    // 看见玩家了
                    targetPlayer.Value = playerTransform.gameObject;
                    return TaskStatus.Success;
                }
            }
        }

        // 没看见玩家
        targetPlayer.Value = null;
        return TaskStatus.Failure;
    }
}