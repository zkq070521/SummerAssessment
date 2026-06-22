using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class InAttackRange : Conditional
{
    public SharedTransform playerTarget;
    public float attackRange = 6f;

    public override TaskStatus OnUpdate()
    {
        if (playerTarget.Value == null)
        {
            Debug.LogWarning("InAttackRange任务失败：playerTarget未设置");
            return TaskStatus.Failure;
        }


        float distance = Vector3.Distance(transform.position, playerTarget.Value.position);

        if (distance <= attackRange)
            return TaskStatus.Success;
        else
            return TaskStatus.Failure;
    }
}