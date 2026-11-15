using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameSceneFlow : MonoBehaviour
{
    public string nextSceneName;
    public float delay = 3f;

    public Action<int> ReturnToStory { get; set; }

    private bool isMoving;

    public void OnGameFinished(int hearts)
    {
        if (isMoving || (ReturnToStory == null && string.IsNullOrWhiteSpace(nextSceneName)))
            return;

        isMoving = true;
        StartCoroutine(MoveNextScene(hearts));
    }

    private IEnumerator MoveNextScene(int hearts)
    {
        yield return new WaitForSeconds(delay);

        if (ReturnToStory != null)
            ReturnToStory(hearts);
        else
            SceneManager.LoadScene(nextSceneName);
    }
}