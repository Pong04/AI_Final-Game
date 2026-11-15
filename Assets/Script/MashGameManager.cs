using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MashGameManager : MonoBehaviour
{
    [Header("UI")]
    public Slider powerGauge;
    public TMP_Text percentText;
    public TMP_Text countText;
    public TMP_Text timerText;

    [Header("결과창")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("게임 설정")]
    [Range(0f, 100f)]
    public float startGauge = 30f;

    public float increasePerPress = 3f;
    public float decreasePerSecond = 15f;

    [Min(0.1f)]
    public float timeLimit = 15f;

    [Header("하트 판정")]
    [Range(0f, 100f)] public float checkpointPercent = 50f;

    public MiniGameSceneFlow sceneFlow;

    private float currentGauge;
    private float remainingTime;
    private int pressCount;
    private int hearts;
    private int nextCheckpoint;
    private bool isFinished;

    private void Start()
    {
        currentGauge = Mathf.Clamp(startGauge, 0f, 100f);
        remainingTime = timeLimit;
        pressCount = 0;
        hearts = StoryRules.FullHearts;
        nextCheckpoint = 1;
        isFinished = false;

        powerGauge.minValue = 0f;
        powerGauge.maxValue = 100f;
        powerGauge.wholeNumbers = false;
        powerGauge.interactable = false;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
        }

        UpdateUI();
    }

    private void Update()
    {
        if (SettingsMenu.BlocksGameplay)
            return;

        bool spacePressed;

#if ENABLE_INPUT_SYSTEM
        spacePressed = Keyboard.current != null &&
                       Keyboard.current.spaceKey.wasPressedThisFrame;

#else
        spacePressed = Input.GetKeyDown(KeyCode.Space);
#endif

        if (isFinished)
            return;

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        // 시간이 흐르면 게이지 감소
        currentGauge = Mathf.Max(
            0f,
            currentGauge - decreasePerSecond * Time.deltaTime
        );

        // 스페이스바를 새로 누를 때마다 증가
        if (spacePressed)
        {
            pressCount++;

            currentGauge = Mathf.Clamp(
                currentGauge + increasePerPress,
                0f,
                100f
            );
        }

        // 제한시간을 세 구간으로 나눠 목표 게이지 미달 시 하트를 잃는다.
        while (nextCheckpoint <= StoryRules.FullHearts &&
               remainingTime <= timeLimit * (StoryRules.FullHearts - nextCheckpoint) / StoryRules.FullHearts)
        {
            if (currentGauge < checkpointPercent)
                hearts--;

            nextCheckpoint++;
        }

        if (remainingTime <= 0f)
        {
            FinishGame();
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        powerGauge.value = currentGauge;

        if (percentText != null)
        {
            percentText.text =
                Mathf.FloorToInt(currentGauge) + "% / 목표 " + checkpointPercent + "%";
        }

        if (countText != null)
        {
            countText.text = "연타 ×" + pressCount + " / 하트 " + hearts;
        }

        if (timerText != null)
        {
            timerText.text =
                "TIME : " + remainingTime.ToString("F1");
        }
    }

    private void FinishGame()
    {
        if (isFinished)
            return;

        isFinished = true;
        UpdateUI();

        Grade grade = StoryRules.GradeForHearts(hearts);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            string title = grade == Grade.F ? "실패" : "클리어!";

            resultText.text =
                title + "\n\n" +
                "등급 : " + grade + "\n" +
                "최종 게이지 : " +
                Mathf.FloorToInt(currentGauge) + "%\n" +
                "연타 횟수 : " + pressCount + "회\n\n";
        }

        if (sceneFlow != null)
        {
            sceneFlow.OnGameFinished(hearts);
        }
    }
}