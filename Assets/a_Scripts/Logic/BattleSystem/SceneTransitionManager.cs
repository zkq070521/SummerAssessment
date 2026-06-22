using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Animator transitionAnim;
    public float transitionDuration = 1.5f;
    public string sceneToLoad = "Battle1";

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
        StartCoroutine(LoadSceneWithTransition());
    }

    IEnumerator LoadSceneWithTransition()
    {
        if (transitionAnim != null)
            transitionAnim.SetTrigger("StartTransition");

        yield return new WaitForSeconds(transitionDuration);

        // 使用 Additive 模式，保留当前场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}