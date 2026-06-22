using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakScreen : MonoBehaviour
{
    public GameObject screenShardsParent;  // 碎玻璃的父物体（初始隐藏）
    public GameObject explosionPosition;   // 爆炸中心点
    public Camera targetCamera;            // 要截屏的摄像机
    public float explosionForce = 3000f;
    public float explosionRadius = 10f;
    public float upwardModifier = 0f;

    private Texture2D capturedTexture;

    // 保存每个碎片的初始状态
    private List<Vector3> initialPositions = new List<Vector3>();
    private List<Quaternion> initialRotations = new List<Quaternion>();
    private bool hasSavedInitialState = false;

    void Start()
    {
        if (screenShardsParent != null)
        {
            screenShardsParent.SetActive(false);
            // 保存初始状态
            SaveInitialState();
        }
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

    // 保存碎片的初始位置和旋转
    private void SaveInitialState()
    {
        if (screenShardsParent == null) return;

        initialPositions.Clear();
        initialRotations.Clear();

        foreach (Transform child in screenShardsParent.transform)
        {
            initialPositions.Add(child.localPosition);
            initialRotations.Add(child.localRotation);
        }

        hasSavedInitialState = true;
    }

    // 重置所有碎片到初始位置
    private void ResetShardsToInitialState()
    {
        if (screenShardsParent == null || !hasSavedInitialState) return;

        int index = 0;
        foreach (Transform child in screenShardsParent.transform)
        {
            if (index < initialPositions.Count)
            {
                child.localPosition = initialPositions[index];
                child.localRotation = initialRotations[index];
            }
            // 重置速度（停止运动）
            if (child.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // 重新启用物理模拟（如果之前被禁用）
                rb.isKinematic = false;
            }
            index++;
        }

    }

    private IEnumerator CaptureAndBreakScreen(Vector3 hitPoint)
    {
        // 先重置碎片到初始位置
        ResetShardsToInitialState();

        // 1. 截屏（捕获当前帧）
        yield return StartCoroutine(CaptureScreen());

        // 2. 应用截图到所有碎玻璃的纹理
        ApplyTextureToShards();

        // 3. 显示碎玻璃（覆盖屏幕）
        if (screenShardsParent != null)
        {
            screenShardsParent.SetActive(true);
        }

        // 4. 等待 4 秒
        yield return new WaitForSeconds(4f);

        // 5. 爆炸！让碎片飞散
        ExplodeShards(hitPoint);

        // 6. 延迟后隐藏父物体（但碎片会继续飞）
        yield return new WaitForSeconds(1f);

        if (screenShardsParent != null)
        {
            screenShardsParent.SetActive(false);
        }
    }

    // 截屏方法
    private IEnumerator CaptureScreen()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;
        targetCamera.Render();

        RenderTexture.active = rt;
        capturedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        capturedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        capturedTexture.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
    }

    // 将截图应用到所有碎玻璃的纹理上
    private void ApplyTextureToShards()
    {
        if (screenShardsParent == null || capturedTexture == null) return;

        MeshRenderer[] renderers = screenShardsParent.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            Material mat = new Material(renderer.sharedMaterial);
            mat.mainTexture = capturedTexture;
            renderer.material = mat;
        }
    }

    // 爆炸碎玻璃
    private void ExplodeShards(Vector3 hitPoint)
    {
        if (screenShardsParent == null) return;

        if (explosionPosition != null)
            explosionPosition.transform.position = hitPoint;

        Vector3 explosionPos = explosionPosition != null ? explosionPosition.transform.position : hitPoint;

        foreach (Transform child in screenShardsParent.transform)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                //  重置速度（防止残留速度影响）
                childRigidbody.velocity = Vector3.zero;
                childRigidbody.angularVelocity = Vector3.zero;

                // 施加爆炸力
                childRigidbody.AddExplosionForce(
                    explosionForce,
                    explosionPos,
                    explosionRadius,
                    upwardModifier,
                    ForceMode.Impulse
                );
                // 加上随机旋转
                //childRigidbody.angularVelocity = Random.insideUnitSphere * 20f;
                //child.parent = null;
            }
        }
    }
}