using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class Patrol : Action
{
    public Transform pointA;
    public Transform pointB;
    public float stoppingDistance = 0.5f;
    public float patrolSpeed = 3.5f;
    public float waitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform currentTarget;
    private float waitTimer;
    private bool isWaiting;
    private float originalSpeed;

    private int speedHash;
    private int isMovingHash;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        speedHash = Animator.StringToHash("Speed");
        isMovingHash = Animator.StringToHash("IsMoving");

        // 1. 检查 NavMeshAgent
        if (agent == null)
        {
            Debug.LogError("❌ 没有 NavMeshAgent 组件！");
            return;
        }


        // 2. 检查路点
        if (pointA == null || pointB == null)
        {
            Debug.LogError($"❌ 路点为空！pointA={pointA?.name}, pointB={pointB?.name}");
            return;
        }
        ;

        // 3. 检查 NavMesh
        if (!agent.isOnNavMesh)
        {

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;

            }
            else
            {
                Debug.LogError("❌ 找不到附近的 NavMesh！请检查是否烘焙了 NavMesh");
                return;
            }
        }
        else
        {
            Debug.Log($"✅ 敌人在 NavMesh 上，位置: {transform.position}");
        }

        originalSpeed = agent.speed;
        agent.speed = patrolSpeed;

        isWaiting = false;
        waitTimer = 0f;
        MoveToPointA();

        if (animator != null)
        {
            animator.SetBool(isMovingHash, true);
        }


    }

    public override TaskStatus OnUpdate()
    {


        if (pointA == null || pointB == null)
        {
            return TaskStatus.Failure;
        }

        if (agent == null)
            return TaskStatus.Failure;

        if (animator != null)
        {
            float normalizedSpeed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat(speedHash, normalizedSpeed);
        }

        // 等待状态
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                if (currentTarget == pointA)
                    MoveToPointB();
                else
                    MoveToPointA();
            }
            return TaskStatus.Running;
        }

        // 移动状态
        if (!agent.pathPending && agent.remainingDistance <= stoppingDistance)
        {
            isWaiting = true;
            waitTimer = waitTime;
            if (animator != null)
            {
                animator.SetFloat(speedHash, 0f);
            }

        }

        return TaskStatus.Running;
    }

    private void MoveToPointA()
    {
        currentTarget = pointA;
        SetDestination(currentTarget.position);

    }

    private void MoveToPointB()
    {
        currentTarget = pointB;
        SetDestination(currentTarget.position);

    }

    private void SetDestination(Vector3 targetPos)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
        {
            bool success = agent.SetDestination(hit.position);

        }
        else
        {
            Debug.LogError($"❌ 目标点不在 NavMesh 上: {targetPos}");
        }
    }

    public override void OnEnd()
    {

        if (agent != null)
            agent.speed = originalSpeed;

        if (animator != null)
        {
            animator.SetFloat(speedHash, 0);
            animator.SetBool(isMovingHash, false);
        }
    }
}