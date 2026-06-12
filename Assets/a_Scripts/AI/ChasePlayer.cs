using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class ChasePlayer : Action
{
    public SharedTransform playerTarget;
    public SharedFloat chaseSpeed = 3f;
    public SharedFloat updateRate = 0.1f;

    private NavMeshAgent agent;
    private Animator animator;
    private float lastUpdateTime;
    private float originalSpeed;

    // 动画参数哈希值
    private int speedHash;
    private int isMovingHash;
    private int isChasingHash;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 缓存动画参数
        speedHash = Animator.StringToHash("Speed");
        isMovingHash = Animator.StringToHash("IsMoving");
        isChasingHash = Animator.StringToHash("IsChasing");

        if (agent == null)
        {
            Debug.LogError("ChasePlayer任务需要NavMeshAgent组件");
            return;
        }

        originalSpeed = agent.speed;
        agent.speed = chaseSpeed.Value;
        lastUpdateTime = Time.time;

        UpdateDestination();

        // 设置追击动画
        if (animator != null)
        {
            animator.SetBool(isMovingHash, true);
            animator.SetBool(isChasingHash, true);
            animator.SetFloat(speedHash, 1.0f); // 追击时全速
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (agent == null || playerTarget.Value == null)
            return TaskStatus.Failure;

        // 更新动画（保持移动状态）
        if (animator != null)
        {
            animator.SetBool(isMovingHash, true);
            animator.SetFloat(speedHash, 1.0f);
        }

        // 定期更新目标位置
        if (Time.time - lastUpdateTime >= updateRate.Value)
        {
            UpdateDestination();
            lastUpdateTime = Time.time;
        }

        return TaskStatus.Running;
    }

    private void UpdateDestination()
    {
        if (playerTarget.Value != null && agent.isOnNavMesh)
        {
            agent.destination = playerTarget.Value.position;
        }
    }

    public override void OnEnd()
    {
        if (agent != null)
            agent.speed = originalSpeed;

        // 重置动画状态
        if (animator != null)
        {
            animator.SetBool(isMovingHash, false);
            animator.SetBool(isChasingHash, false);
            animator.SetFloat(speedHash, 0);
        }
    }
}