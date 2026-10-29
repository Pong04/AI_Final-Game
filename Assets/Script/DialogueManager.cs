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

    public GameObject choicePanel;

    // 첫 번째 선택지를 골랐을 때 나올 대사
    public DialogueLine[] firstBranch;

    // 두 번째 선택지를 골랐을 때 나올 대사
    public DialogueLine[] secondBranch;

    private bool isChoosing = false;
    private bool hasChosen = false;

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
        choicePanel.SetActive(false);
        isChoosing = false;
        hasChosen = false;

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
        if (isFinished || isChoosing)
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
        // 공통 대사가 끝나면 선택지를 표시
        if (!hasChosen)
        {
            isChoosing = true;
            choicePanel.SetActive(true);
            return;
        }

        // 선택한 분기의 대사까지 끝나면 대화 종료
        isFinished = true;
        dialoguePanel.SetActive(false);

        Debug.Log("분기 대화 종료");
    }

    public void SelectChoice(int choiceIndex)
    {
        // 선택지가 떠 있을 때만 작동
        if (!isChoosing)
            return;

        if (choiceIndex != 0 && choiceIndex != 1)
            return;

        choicePanel.SetActive(false);

        isChoosing = false;
        hasChosen = true;
        isFinished = false;

        // 선택에 따라 사용할 대사 목록 변경
        lines = choiceIndex == 0 ? firstBranch : secondBranch;

        currentIndex = 0;

        if (lines == null || lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        dialoguePanel.SetActive(true);
        ShowLine();
    }
}