using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 碎屏特效 — 截取当前画面铺成碎玻璃网格，再定住停留（露出背后的黑幕）。
///
/// 由 SceneTransitionManager 编排调用：ShowAsync() 铺碎片 → Break() 定住停留 → Hide() 复原。
/// 自身不再订阅 OnHitEnemy，避免与场景切换的时序打架。
/// </summary>
public class BreakScreen : MonoBehaviour
{
    public GameObject screenShardsParent;  // 碎玻璃的父物体（初始隐藏）
    public Camera targetCamera;            // 要截屏的摄像机

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

    /// <summary>截取当前画面并铺成碎玻璃（协程，内部等待一帧完成截屏）</summary>
    public IEnumerator ShowAsync()
    {
        if (screenShardsParent == null) yield break;

        ResetShardsToInitialState();
        yield return CaptureScreen();
        ApplyTextureToShards();
        screenShardsParent.SetActive(true);
    }

    /// <summary>碎开并停留：把碎片定住（不移动、不下落），保持摆放好的碎屏画面</summary>
    public void Break() => FreezeShards();

    /// <summary>隐藏并复原碎片（下次播放前调用）</summary>
    public void Hide()
    {
        if (screenShardsParent != null)
            screenShardsParent.SetActive(false);
        ResetShardsToInitialState();
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

    // 截屏方法（URP 兼容）
    private IEnumerator CaptureScreen()
    {
        // 销毁上次的截图纹理
        if (capturedTexture != null)
            Destroy(capturedTexture);

        int width = Screen.width;
        int height = Screen.height;

        // 先挂 RT，让 URP 管线自动渲染，不手动 camera.Render()
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        targetCamera.targetTexture = rt;

        yield return new WaitForEndOfFrame();

        RenderTexture.active = rt;
        capturedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        capturedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        capturedTexture.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
    }

    // 将截图应用到所有碎玻璃的纹理上（覆盖全部材质槽/子网格，避免个别碎片因正面在材质槽 1 而漏贴）
    private void ApplyTextureToShards()
    {
        if (screenShardsParent == null || capturedTexture == null) return;

        MeshRenderer[] renderers = screenShardsParent.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            Material[] sharedMats = renderer.sharedMaterials;
            Material[] mats = new Material[sharedMats.Length];

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] == null)
                {
                    mats[i] = null;
                    continue;
                }

                Material mat = new Material(sharedMats[i]);
                mat.mainTexture = capturedTexture;           // Built-in RP
                mat.SetTexture("_BaseMap", capturedTexture); // URP Lit 实际采样名
                mats[i] = mat;
            }

            renderer.materials = mats;
        }
    }

    // 把碎片定住（关掉物理），让它们停在摆放好的位置，不移动、不下落
    private void FreezeShards()
    {
        if (screenShardsParent == null) return;

        foreach (Transform child in screenShardsParent.transform)
        {
            if (!child.TryGetComponent<Rigidbody>(out Rigidbody rb)) continue;

            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
