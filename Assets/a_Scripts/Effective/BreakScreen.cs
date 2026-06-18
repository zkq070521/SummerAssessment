using System.Collections;
using UnityEngine;

public class BreakScreen : MonoBehaviour
{
    public GameObject screenShardsParent;  // 碎玻璃的父物体（初始隐藏）
    public GameObject explosionPosition;   // 爆炸中心点
    public Camera targetCamera;            // 要截屏的摄像机（跟踪玩家的那个）
    public float explosionForce = 1000f;
    public float explosionRadius = 5f;
    public float upwardModifier = 3f;

    private Texture2D capturedTexture;
    private Material[] shardMaterials;

    void Start()
    {
        // 初始隐藏碎玻璃
        if (screenShardsParent != null)
            screenShardsParent.SetActive(false);
    }

    void OnEnable()
    {
        GameEvents.OnHitEnemy += OnHitEnemyHandler;
    }

    void OnDisable()
    {
        GameEvents.OnHitEnemy -= OnHitEnemyHandler;
    }

    private void OnHitEnemyHandler(GameObject enemy, Vector3 hitPoint)
    {
        StartCoroutine(CaptureAndBreakScreen(hitPoint));
    }

    private IEnumerator CaptureAndBreakScreen(Vector3 hitPoint)
    {
        // 1. 截屏（捕获当前帧）
        yield return StartCoroutine(CaptureScreen());

        // 2. 应用截图到所有碎玻璃的纹理
        ApplyTextureToShards();

        // 3. 显示碎玻璃（覆盖屏幕）
        if (screenShardsParent != null)
        {
            Debug.Log("Activating screen shards parent");
            screenShardsParent.SetActive(true);
        }


        // 4. 稍微延迟后爆炸
        yield return new WaitForSeconds(3f);

        // 5. 爆炸！让碎片飞散
        ExplodeShards(hitPoint);

        // 6. 可选：延迟后隐藏碎片并恢复游戏
        yield return new WaitForSeconds(3f);
        if (screenShardsParent != null)
            screenShardsParent.SetActive(false);
    }

    // 截屏方法
    private IEnumerator CaptureScreen()
    {
        // 等待一帧，确保渲染完成
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;

        // 创建 RenderTexture 并截屏
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;
        targetCamera.Render();

        RenderTexture.active = rt;
        capturedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        capturedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        capturedTexture.Apply();

        // 清理
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
    }

    // 将截图应用到所有碎玻璃的纹理上
    private void ApplyTextureToShards()
    {
        if (screenShardsParent == null || capturedTexture == null) return;

        // 获取所有 MeshRenderer
        MeshRenderer[] renderers = screenShardsParent.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            // 为每个碎片创建一个独立的 Material 实例（防止共享）
            Material mat = new Material(renderer.sharedMaterial);
            mat.mainTexture = capturedTexture;
            renderer.material = mat;
        }
    }

    // 爆炸碎玻璃
    private void ExplodeShards(Vector3 hitPoint)
    {
        if (screenShardsParent == null) return;

        // 把爆炸位置移到命中点
        if (explosionPosition != null)
            explosionPosition.transform.position = hitPoint;

        Vector3 explosionPos = explosionPosition != null ? explosionPosition.transform.position : hitPoint;

        foreach (Transform child in screenShardsParent.transform)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                // 施加爆炸力
                childRigidbody.AddExplosionForce(
                    explosionForce,
                    explosionPos,
                    explosionRadius,
                    upwardModifier
                );

                // 让碎片脱离父物体（独立运动）
                child.parent = null;

                // 可选：添加随机旋转
                childRigidbody.AddTorque(
                    Random.insideUnitSphere * 100f
                );
            }
        }

        // 清空父物体（防止残留）
        screenShardsParent.SetActive(false);
    }
}