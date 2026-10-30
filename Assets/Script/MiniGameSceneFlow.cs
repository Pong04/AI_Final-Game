using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameSceneFlow : MonoBehaviour
{
    public string nextSceneName;
    public float delay = 3f;

    private bool isMoving;

    public void OnGameFinished()
    {
        if (isMoving || string.IsNullOrWhiteSpace(nextSceneName))
            return;

        isMoving = true;
        StartCoroutine(MoveNextScene());
    }

    private IEnumerator MoveNextScene()
    {
        yield return new WaitForSecondsRealtime(delay);

        SceneManager.LoadScene(nextSceneName);
    }
}