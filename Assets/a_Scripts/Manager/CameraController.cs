using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public CinemachineFreeLook followCam;
    public CinemachineVirtualCamera hitCam;
    public float hitCameraDuration = 3f;

    void OnEnable()
    {
        // 订阅事件
        GameEvents.OnHitEnemy += OnHitEnemyHandler;
    }

    void OnDisable()
    {
        // 取消订阅（防止内存泄漏）
        GameEvents.OnHitEnemy -= OnHitEnemyHandler;
    }

    void Start()
    {
        followCam.Priority = 10;
        hitCam.Priority = 0;
    }

    private void OnHitEnemyHandler(GameObject enemy, Vector3 hitPoint)
    {
        // 把特写镜头对准敌人
        // hitCam.Follow = enemy.transform;
        // hitCam.LookAt = enemy.transform;

        // 切换到特写镜头
        // followCam.Priority = 0;
        // hitCam.Priority = 10;

        // 延迟后切回
        //Invoke(nameof(SwitchToFollowCamera), hitCameraDuration);
    }

    // private void SwitchToFollowCamera()
    // {
    //     followCam.Priority = 10;
    //     hitCam.Priority = 0;
    // }
}