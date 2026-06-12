using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class ChasePlayer : Action
{
    public SharedTransform playerTarget;      // 玩家Transform
    public SharedFloat chaseSpeed = 5f;       // 追击速度
    public SharedFloat updateRate = 0.1f;     // 路径更新频率

    private NavMeshAgent agent;
    private float lastUpdateTime;
    private float originalSpeed;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("ChasePlayer任务需要NavMeshAgent组件");
            return;
        }

        originalSpeed = agent.speed;
        agent.speed = chaseSpeed.Value;
        lastUpdateTime = Time.time;

        // 立即设置目标
        UpdateDestination();
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null || playerTarget.Value == null)
            return TaskStatus.Failure;

        // 定期更新目标位置
        if (Time.time - lastUpdateTime >= updateRate.Value)
        {
            UpdateDestination();
            lastUpdateTime = Time.time;
        }

        // 检查是否到达玩家附近（可选：距离小于攻击范围时可返回成功）
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            return TaskStatus.Running;  // 保持追击状态，持续跟随
        }

        return TaskStatus.Running;
    }

    private void UpdateDestination()
    {
        if (playerTarget.Value != null)
        {
            agent.destination = playerTarget.Value.position;
        }
    }

    public override void OnEnd()
    {
        // 恢复巡逻速度
        if (agent != null)
            agent.speed = originalSpeed;
    }
}