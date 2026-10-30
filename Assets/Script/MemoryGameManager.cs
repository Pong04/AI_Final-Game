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
    public TMP_Text resultText;

    [Header("설정")]
    public float flipBackDelay = 0.8f;

    // 서로 다른 단어 8개를 입력해줘.
    public string[] words =
    {
        "사과", "바다", "학교", "게임",
        "고양이", "노트북", "하늘", "친구"
    };

    private MemoryCard firstCard;
    private MemoryCard secondCard;

    private int matchedPairs;
    private int attempts;

    private bool isChecking;
    private bool isFinished;

    private void Start()
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }

        if (!ValidateWords())
        {
            isFinished = true;

            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text = "서로 다른 단어 8개를 입력해주세요.";
            }

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

        // 같은 단어를 두 장씩 추가 → 총 16장
        foreach (string word in words)
        {
            deck.Add(word.Trim());
            deck.Add(word.Trim());
        }

        // 카드 순서를 랜덤으로 섞기
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            string temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }

        // 카드판 안에 생성
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
            UpdateUI();

            if (matchedPairs == 8)
            {
                FinishGame();
            }
        }
        else
        {
            // 틀렸을 때 두 단어를 잠깐 보여준 뒤 뒤집기
            yield return new WaitForSeconds(flipBackDelay);

            firstCard.Hide();
            secondCard.Hide();
        }

        firstCard = null;
        secondCard = null;
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
            attemptText.text = "시도 : " + attempts;
        }
    }

    private void FinishGame()
    {
        isFinished = true;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "SUCCESS!\n총 " + attempts + "번 시도";
        }
    }
}