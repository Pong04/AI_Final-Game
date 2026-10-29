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
    public TMP_Text resultText;

    [Header("게임 설정")]
    [Range(0f, 100f)]
    public float startGauge = 30f;

    public float increasePerPress = 5f;
    public float decreasePerSecond = 10f;
    public float timeLimit = 15f;

    private float currentGauge;
    private float remainingTime;
    private int pressCount;
    private bool isFinished;

    private void Start()
    {
        currentGauge = Mathf.Clamp(startGauge, 0f, 100f);
        remainingTime = Mathf.Max(0f, timeLimit);
        pressCount = 0;
        isFinished = false;

        powerGauge.minValue = 0f;
        powerGauge.maxValue = 100f;
        powerGauge.wholeNumbers = false;
        powerGauge.interactable = false;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
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
        {
            if (restartPressed)
            {
                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name
                );
            }

            return;
        }

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        // 제한시간이 끝나면 실패
        if (remainingTime <= 0f)
        {
            FinishGame(false);
            return;
        }

        // 매초 일정량 감소
        currentGauge = Mathf.Max(
            0f,
            currentGauge - decreasePerSecond * Time.deltaTime
        );

        // 누르고 있는 동안이 아니라 새로 누를 때만 증가
        if (spacePressed)
        {
            pressCount++;

            currentGauge = Mathf.Clamp(
                currentGauge + increasePerPress,
                0f,
                100f
            );
        }

        if (currentGauge >= 100f)
        {
            FinishGame(true);
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        powerGauge.value = currentGauge;

        if (percentText != null)
        {
            // 성공하기 전에는 100%로 반올림되지 않게 처리
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

    private void FinishGame(bool success)
    {
        isFinished = true;
        UpdateUI();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            resultText.text = success
                ? "SUCCESS!\nPress R to restart"
                : "GAME OVER\nPress R to restart";
        }

        Debug.Log(success ? "전원 복구 성공!" : "전원 복구 실패!");
    }
}