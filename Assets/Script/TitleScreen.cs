using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField, Min(0f)] private float openingDuration = 60f;

    private static bool openingSeen;
    private bool showingOpening;
    private bool loadingStory;
    private Coroutine opening;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOpening()
    {
        openingSeen = false;
    }

    private void Start()
    {
        if (openingSeen || openingDuration <= 0f)
        {
            ShowTitle();
            return;
        }

        showingOpening = true;
        titleLabel.text = "EP.0\n학사관리처에서 알립니다.\n졸업까지 3학점이 부족합니다.";
        buttonLabel.text = "건너뛰기";
        opening = StartCoroutine(FinishOpening());
    }

    private IEnumerator FinishOpening()
    {
        yield return new WaitForSeconds(openingDuration);
        opening = null;
        ShowTitle();
    }

    private void ShowTitle()
    {
        openingSeen = true;
        showingOpening = false;
        titleLabel.text = "무사히 졸업";
        buttonLabel.text = "게임 시작";
        SettingsMenu.ShowTitleButton(titleLabel.font);
    }

    public void Advance()
    {
        if (SettingsMenu.BlocksGameplay)
            return;

        if (showingOpening)
        {
            if (opening != null)
                StopCoroutine(opening);

            opening = null;
            ShowTitle();
            return;
        }

        if (loadingStory)
            return;

        loadingStory = true;
        SceneManager.LoadScene("Story_S");
    }
}
