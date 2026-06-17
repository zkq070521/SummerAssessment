using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public Collider weaponCollider; // 挂载在武器上的触发器

    void Start()
    {
        // 初始状态关闭触发器，防止误触
        weaponCollider.enabled = false;
    }

    // 由动画事件调用，在攻击判定帧开启
    public void EnableWeapon()
    {
        weaponCollider.enabled = true;
    }

    // 由动画事件调用，在攻击判定结束后关闭
    public void DisableWeapon()
    {
        weaponCollider.enabled = false;
    }

    // 当触发器碰见其他物体时调用
    void OnTriggerEnter(Collider other)
    {
        // 判断碰到的是不是敌人（通过Tag或Layer）
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Hit enemy: ");
            // 获取敌人的受伤脚本，扣减血量
            // EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            // if (enemy != null)
            // {
            //     enemy.TakeDamage(damage);
            // }
        }
    }
}