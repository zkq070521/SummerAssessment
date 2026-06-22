using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class Attack : Action
{
    public SharedTransform playerTarget;
    public SharedFloat attackRate = 1f;      // 攻击频率（次/秒）
    public SharedFloat attackDamage = 10f;
    public SharedFloat attackRange = 1.5f;

    private Animator animator;
    private float lastAttackTime;
    private int attackHash;

    public override void OnStart()
    {
        animator = GetComponent<Animator>();
        attackHash = Animator.StringToHash("Attack");
        lastAttackTime = -attackRate.Value; // 确保可以立即攻击
    }

    public override TaskStatus OnUpdate()
    {
        if (playerTarget.Value == null)
            return TaskStatus.Failure;



        // 攻击冷却检查
        if (Time.time >= lastAttackTime + attackRate.Value)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }

        // 攻击状态下保持面向玩家
        Vector3 directionToPlayer = (playerTarget.Value.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        return TaskStatus.Running;
    }

    private void PerformAttack()
    {
        // 触发攻击动画
        if (animator != null)
        {
            animator.SetTrigger(attackHash);
        }

        // 造成伤害（假设玩家有Health组件）
        if (playerTarget.Value != null)
        {
            Debug.Log($"{gameObject.name} 攻击了玩家，造成 {attackDamage.Value} 伤害");
            // Health playerHealth = playerTarget.Value.GetComponent<Health>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(attackDamage.Value);
            //     Debug.Log($"{gameObject.name} 攻击了玩家，造成 {attackDamage.Value} 伤害");
            // }
        }

        // 可选：播放攻击音效、特效等
        // AudioSource.PlayClipAtPoint(attackSound, transform.position);
    }

    public override void OnEnd()
    {
        // 确保攻击动画状态结束（可选）
        if (animator != null)
        {
            animator.ResetTrigger(attackHash);
        }
    }
}