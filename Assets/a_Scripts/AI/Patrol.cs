using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class Patrol : Action
{
    public SharedTransform[] waypoints;      // 路点数组
    public SharedFloat stoppingDistance = 0.5f;  // 到达距离阈值
    public SharedFloat patrolSpeed = 2f;      // 巡逻速度

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private float originalSpeed;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Patrol任务需要NavMeshAgent组件");
            return;
        }

        originalSpeed = agent.speed;
        agent.speed = patrolSpeed.Value;

        // 如果没有设置起始索引或已完成所有路点，从第一个开始
        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;
        }

        // 设置初始目标
        SetDestination();
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null || waypoints.Length == 0)
            return TaskStatus.Failure;

        // 检查是否到达目标路点
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance.Value)
        {
            // 移动到下一个路点
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            SetDestination();
        }

        // 检查Agent是否还在移动
        if (agent.remainingDistance > stoppingDistance.Value)
            return TaskStatus.Running;

        return TaskStatus.Running;
    }

    private void SetDestination()
    {
        if (waypoints[currentWaypointIndex].Value != null)
        {
            agent.destination = waypoints[currentWaypointIndex].Value.position;
        }
    }

    public override void OnEnd()
    {
        // 恢复原始速度（用于追击时）
        if (agent != null)
            agent.speed = originalSpeed;
    }
}