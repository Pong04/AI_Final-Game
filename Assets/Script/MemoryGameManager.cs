using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MemoryGameManager : MonoBehaviour
{
    [Header("카드 연결")]
    public Transform cardBoard;
    public MemoryCard cardPrefab;

    [Header("UI")]
    public TMP_Text pairText;
    public TMP_Text attemptText;

    [Header("결과창")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("게임 설정")]
    public float flipBackDelay = 0.8f;

    [Header("등급 설정")]
    [Min(8)]
    public int aGradeMaxAttempts = 12;

    [Min(8)]
    public int maxAttempts = 20;

    [Header("서로 다른 단어 8개")]
    public string[] words =
    {
        "사과", "바다", "학교", "게임",
        "고양이", "노트북", "하늘", "친구"
    };

    public MiniGameSceneFlow sceneFlow;

    private MemoryCard firstCard;
    private MemoryCard secondCard;

    private int matchedPairs;
    private int attempts;

    private bool isChecking;
    private bool isFinished;

    private void Start()
    {
        maxAttempts = Mathf.Max(8, maxAttempts);

        aGradeMaxAttempts = Mathf.Clamp(
            aGradeMaxAttempts,
            8,
            maxAttempts
        );

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
        }

        if (!ValidateWords())
        {
            isFinished = true;
            ShowResult("서로 다른 단어 8개를 입력해주세요.");
            return;
        }

        CreateCards();
        UpdateUI();
    }

    private bool ValidateWords()
    {
        if (words == null || words.Length != 8)
        {
            Debug.LogError("Words에는 단어 8개가 필요합니다.");
            return false;
        }

        HashSet<string> uniqueWords = new HashSet<string>();

        foreach (string word in words)
        {
            if (string.IsNullOrWhiteSpace(word) ||
                !uniqueWords.Add(word.Trim()))
            {
                Debug.LogError(
                    "빈 단어 없이 서로 다른 단어 8개를 입력해주세요."
                );

                return false;
            }
        }

        return true;
    }

    private void CreateCards()
    {
        List<string> deck = new List<string>();

        foreach (string word in words)
        {
            deck.Add(word.Trim());
            deck.Add(word.Trim());
        }

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            string temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        foreach (string word in deck)
        {
            MemoryCard card = Instantiate(cardPrefab, cardBoard);
            card.Initialize(word, this);
        }
    }

    public void SelectCard(MemoryCard card)
    {
        if (isFinished || isChecking ||
            card.IsRevealed || card.IsMatched)
        {
            return;
        }

        card.Reveal();

        if (firstCard == null)
        {
            firstCard = card;
            return;
        }

        secondCard = card;
        attempts++;

        UpdateUI();
        StartCoroutine(CheckPair());
    }

    private IEnumerator CheckPair()
    {
        isChecking = true;

        if (firstCard.Word == secondCard.Word)
        {
            firstCard.SetMatched();
            secondCard.SetMatched();

            matchedPairs++;
        }
        else
        {
            yield return new WaitForSeconds(flipBackDelay);

            firstCard.Hide();
            secondCard.Hide();
        }

        firstCard = null;
        secondCard = null;

        UpdateUI();

        // 마지막 허용 시도에 모두 맞추면 성공
        if (matchedPairs == 8)
        {
            FinishGame(true);
        }
        else if (attempts >= maxAttempts)
        {
            FinishGame(false);
        }

        isChecking = false;
    }

    private void UpdateUI()
    {
        if (pairText != null)
        {
            pairText.text = "짝 : " + matchedPairs + " / 8";
        }

        if (attemptText != null)
        {
            attemptText.text =
                "시도 : " + attempts + " / " + maxAttempts;
        }
    }

    private void FinishGame(bool success)
    {
        if (isFinished)
            return;

        isFinished = true;

        string grade;

        if (!success)
        {
            grade = "F";
        }
        else if (attempts <= aGradeMaxAttempts)
        {
            grade = "A";
        }
        else
        {
            grade = "B";
        }

        string title = success ? "클리어!" : "실패";

        ShowResult(
            title + "\n\n" +
            "등급 : " + grade + "\n" +
            "맞춘 짝 : " + matchedPairs + " / 8\n" +
            "총 시도 : " + attempts + "회"
        );

        if (sceneFlow != null)
        {
            sceneFlow.OnGameFinished();
        }
    }

    private void ShowResult(string message)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = message;
        }
    }
}