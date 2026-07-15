using UnityEngine;

public class FadeFromBottomControl : MonoBehaviour
{
    public Material targetMaterial;
    public float fadeDuration = 2f;

    private float startTime;

    void Start()
    {
        startTime = Time.time;
        targetMaterial.SetFloat("_FadeStart", -1f);
        targetMaterial.SetFloat("_FadeEnd", -0.5f);
    }

    void Update()
    {
        float progress = (Time.time - startTime) / fadeDuration;
        if (progress <= 1f)
        {
            float bottom = Mathf.Lerp(-1f, 1f, progress);
            float top = bottom + 0.5f;
            targetMaterial.SetFloat("_FadeStart", bottom);
            targetMaterial.SetFloat("_FadeEnd", top);
        }
    }
}