using System.Collections;
using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string dialogue;
}

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public DialogueLine[] lines;

    [Min(0.001f)]
    public float typingSpeed = 0.05f;

    private int currentIndex;
    private bool isFinished;
    private bool isTyping;
    private Coroutine typingCoroutine;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
    if (Keyboard.current != null &&
        Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        NextDialogue();
    }
#else
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextDialogue();
        }
#endif
    }

    private void Start()
    {
        currentIndex = 0;
        isFinished = false;

        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        dialoguePanel.SetActive(true);
        ShowLine();
    }

    private void ShowLine()
    {
        nameText.text = lines[currentIndex].speaker;

        // 전체 대사를 넣고 보이는 글자 수만 늘려줘.
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.text = lines[currentIndex].dialogue;
        dialogueText.ForceMeshUpdate();

        isTyping = true;
        typingCoroutine = StartCoroutine(TypeDialogue());
    }

    private IEnumerator TypeDialogue()
    {
        int totalCharacters = dialogueText.textInfo.characterCount;

        for (int i = 1; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            if (i < totalCharacters)
            {
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void NextDialogue()
    {
        if (isFinished)
            return;

        // 타이핑 중 클릭하면 현재 대사를 전부 표시해.
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        currentIndex++;

        if (currentIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    private void EndDialogue()
    {
        isFinished = true;
        dialoguePanel.SetActive(false);

        // 나중에 미니게임 시작 기능을 연결할 위치.
        Debug.Log("대화 종료");
    }
}