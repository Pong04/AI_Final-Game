using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    [Header("등급 설정")]
    [Range(0f, 100f)]
    public float aGradePercent = 80f;

    [Range(0f, 100f)]
    public float bGradePercent = 50f;

    public MiniGameSceneFlow sceneFlow;

    private float currentGauge;
    private float remainingTime;
    private int pressCount;
    private bool isFinished;

    private void Start()
    {
        currentGauge = Mathf.Clamp(startGauge, 0f, 100f);
        remainingTime = timeLimit;
        pressCount = 0;
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
        bool spacePressed;
        bool restartPressed;

#if ENABLE_INPUT_SYSTEM
        spacePressed = Keyboard.current != null &&
                       Keyboard.current.spaceKey.wasPressedThisFrame;

        restartPressed = Keyboard.current != null &&
                         Keyboard.current.rKey.wasPressedThisFrame;
#else
        spacePressed = Input.GetKeyDown(KeyCode.Space);
        restartPressed = Input.GetKeyDown(KeyCode.R);
#endif

        if (isFinished)
            return;

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        if (remainingTime <= 0f)
        {
            FinishGame();
            return;
        }

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

        UpdateUI();
    }

    private void UpdateUI()
    {
        powerGauge.value = currentGauge;

        if (percentText != null)
        {
            percentText.text =
                Mathf.FloorToInt(currentGauge) + "%";
        }

        if (countText != null)
        {
            countText.text = "연타 x" + pressCount;
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

        string grade;

        if (currentGauge >= aGradePercent)
        {
            grade = "A";
        }
        else if (currentGauge >= bGradePercent)
        {
            grade = "B";
        }
        else
        {
            grade = "F";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            string title = grade == "F" ? "실패" : "클리어!";

            resultText.text =
                title + "\n\n" +
                "등급 : " + grade + "\n" +
                "최종 게이지 : " +
                Mathf.FloorToInt(currentGauge) + "%\n" +
                "연타 횟수 : " + pressCount + "회\n\n";
        }

        if (sceneFlow != null)
        {
            sceneFlow.OnGameFinished();
        }
    }
}