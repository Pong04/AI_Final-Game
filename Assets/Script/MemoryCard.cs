using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryCard : MonoBehaviour
{
    public Button cardButton;
    public TMP_Text wordText;
    public Image cardImage;

    public Color backColor = new Color(0.2f, 0.3f, 0.5f);
    public Color frontColor = Color.white;
    public Color matchedColor = new Color(0.5f, 0.9f, 0.6f);

    public string Word { get; private set; }
    public bool IsRevealed { get; private set; }
    public bool IsMatched { get; private set; }

    private MemoryGameManager manager;

    public void Initialize(
        string word,
        MemoryGameManager gameManager)
    {
        Word = word;
        manager = gameManager;
        IsMatched = false;

        cardButton.onClick.AddListener(OnClicked);

        Hide();
    }

    private void OnClicked()
    {
        manager.SelectCard(this);
    }

    public void Reveal()
    {
        IsRevealed = true;

        wordText.text = Word;
        wordText.color = Color.black;

        cardImage.color = frontColor;
        cardButton.interactable = false;
    }

    public void Hide()
    {
        IsRevealed = false;

        wordText.text = "?";
        wordText.color = Color.white;

        cardImage.color = backColor;
        cardButton.interactable = true;
    }

    public void SetMatched()
    {
        IsMatched = true;
        cardImage.color = matchedColor;
        cardButton.interactable = false;
    }
}