using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TypingGameManager : MonoBehaviour
{
    [Header("UI 연결")]
    public RectTransform wordArea;
    public TMP_Text wordPrefab;
    public TMP_InputField typingInput;

    public TMP_Text livesText;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text resultText;

    [Header("게임 설정")]
    public int maxLives = 3;
    public float playTime = 60f;

    [Min(0.1f)]
    public float spawnInterval = 2f;

    public float fallSpeed = 80f;

    [Header("출제 단어")]
    public string[] words =
    {
        "졸업유예", "초과학기", "화석", "낙제",
        "팀플", "과제", "시간", "대학원",
        "취업", "자소서", "교수님면담", "성적"
    };

    private class FallingWord
    {
        public TMP_Text view;
        public string answer;
    }

    private readonly List<FallingWord> activeWords =
        new List<FallingWord>();

    private int currentLives;
    private int score;
    private float remainingTime;
    private float spawnTimer;
    private bool isFinished;
    private bool isSubmitting;

    private void Start()
    {
        currentLives = maxLives;
        remainingTime = playTime;
        score = 0;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }

        typingInput.lineType = TMP_InputField.LineType.SingleLine;
        typingInput.onSubmit.AddListener(SubmitWord);
        typingInput.SetTextWithoutNotify("");

        UpdateUI();
        SpawnWord();

        typingInput.ActivateInputField();
    }

    private void Update()
    {
        if (isFinished)
            return;

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        // 단어를 아래로 이동
        for (int i = activeWords.Count - 1; i >= 0; i--)
        {
            FallingWord word = activeWords[i];
            RectTransform rect = word.view.rectTransform;

            rect.anchoredPosition +=
                Vector2.down * fallSpeed * Time.deltaTime;

            // 단어 아래쪽이 영역 바닥에 닿으면 목숨 감소
            if (rect.anchoredPosition.y -
                rect.rect.height * 0.5f <= 0f)
            {
                RemoveWord(i);
                currentLives--;

                if (currentLives <= 0)
                {
                    FinishGame(false);
                    return;
                }
            }
        }

        if (remainingTime <= 0f)
        {
            FinishGame(true);
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= Mathf.Max(0.1f, spawnInterval))
        {
            spawnTimer = 0f;
            SpawnWord();
        }

        UpdateUI();
    }

    private void SpawnWord()
    {
        if (words == null || words.Length == 0)
            return;

        string answer = words[Random.Range(0, words.Length)];

        if (string.IsNullOrWhiteSpace(answer))
            return;

        TMP_Text view = Instantiate(wordPrefab, wordArea);
        view.gameObject.SetActive(true);
        view.text = answer.Trim();

        RectTransform rect = view.rectTransform;

        // WordArea의 왼쪽 아래를 좌표 기준으로 사용
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        float halfWidth = rect.rect.width * 0.5f;
        float halfHeight = rect.rect.height * 0.5f;
        float areaWidth = wordArea.rect.width;

        float x = areaWidth <= rect.rect.width
            ? areaWidth * 0.5f
            : Random.Range(halfWidth, areaWidth - halfWidth);

        rect.anchoredPosition = new Vector2(
            x,
            wordArea.rect.height - halfHeight
        );

        activeWords.Add(new FallingWord
        {
            view = view,
            answer = answer.Trim()
        });
    }

    private void SubmitWord(string submittedText)
    {
        if (isFinished || isSubmitting)
            return;

        StartCoroutine(CheckAnswer(submittedText));
    }

    private IEnumerator CheckAnswer(string submittedText)
    {
        isSubmitting = true;

        // 입력창의 해당 프레임 처리가 끝난 뒤 확인
        yield return null;

        if (isFinished)
        {
            isSubmitting = false;
            yield break;
        }

        string answer = typingInput.text.Trim();

        if (string.IsNullOrEmpty(answer))
        {
            answer = submittedText.Trim();
        }

        int targetIndex = -1;
        float lowestY = float.MaxValue;

        for (int i = 0; i < activeWords.Count; i++)
        {
            if (activeWords[i].answer != answer)
                continue;

            float y = activeWords[i].view
                .rectTransform.anchoredPosition.y;

            // 동일한 단어 중 가장 아래의 단어 선택
            if (y < lowestY)
            {
                lowestY = y;
                targetIndex = i;
            }
        }

        if (targetIndex >= 0)
        {
            RemoveWord(targetIndex);
            score++;
        }

        typingInput.SetTextWithoutNotify("");
        typingInput.ActivateInputField();

        UpdateUI();
        isSubmitting = false;
    }

    private void RemoveWord(int index)
    {
        Destroy(activeWords[index].view.gameObject);
        activeWords.RemoveAt(index);
    }

    private void UpdateUI()
    {
        if (livesText != null)
            livesText.text = "LIFE : " + currentLives;

        if (timerText != null)
            timerText.text =
                "TIME : " + Mathf.CeilToInt(remainingTime);

        if (scoreText != null)
            scoreText.text = "SCORE : " + score;
    }

    private void FinishGame(bool success)
    {
        isFinished = true;

        typingInput.DeactivateInputField();
        typingInput.interactable = false;

        UpdateUI();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = success
                ? "SUCCESS!"
                : "GAME OVER";
        }
    }

    private void OnDestroy()
    {
        if (typingInput != null)
        {
            typingInput.onSubmit.RemoveListener(SubmitWord);
        }
    }
}