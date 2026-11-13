using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

// 미니게임 등급
public enum Grade { A, B, F }

[System.Serializable]
public class DialogueLine
{
    public string speaker;              // 비워두면 지문(나레이션)으로 표시

    [TextArea(2, 5)]
    public string dialogue;
}

[System.Serializable]
public class ChoiceOption
{
    public string text;                 // 버튼에 표시될 글자
    public DialogueLine[] lines;        // 이 선택지를 골랐을 때 나오는 대사
}

// 에피소드 진행 순서:
// 시작 대사 → (선택지 → 선택 결과) → 미니게임 전 대사 → 미니게임
// → 결과 화면 대사 → 등급별 대사(A/B/F) → (F면 중도 엔딩) → 마무리 대사
[System.Serializable]
public class Episode
{
    public string episodeName;

    public DialogueLine[] introLines;       // 시작 대사
    public ChoiceOption[] choices;          // 선택지 (비워두면 선택지 없음, 최대 버튼 개수만큼)
    public DialogueLine[] afterChoiceLines; // 선택 후 ~ 미니게임 전 대사

    public bool hasMinigame;
    public string gradeName;                // 성적표에 나올 이름 (출석, 조별과제 …)
    public DialogueLine[] resultIntroLines; // "결과 화면 – 등급 공개" 같은 대사
    public DialogueLine[] resultA;
    public DialogueLine[] resultB;
    public DialogueLine[] resultF;

    public bool failEndsGame;               // 체크하면 F일 때 중도 엔딩으로 끝남
    public DialogueLine[] failEndingLines;  // 중도 엔딩 대사

    public DialogueLine[] outroLines;       // 마무리 대사 (EP 클리어!)
}

[System.Serializable]
public class StoryData
{
    public Episode[] episodes;

    // 최종 엔딩 (모든 에피소드가 끝난 뒤)
    public DialogueLine[] finalIntroLines;  // 성적표 공개
    public DialogueLine[] endingGraduate;   // ENDING 01 무사히 졸업
    public DialogueLine[] endingRepeat;     // ENDING 02 한 학기만 더...
    public DialogueLine[] endingExcellent;  // ENDING 03 끝나지 않은 대학 생활
}

public class DialogueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject dialoguePanel;
    public GameObject namePanel;            // 이름칸 배경 (선택 사항, 지문일 때 숨김)
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    public GameObject choicePanel;
    public GameObject[] choiceButtons;      // 선택지 버튼 (EP.1이 3개라서 3개 필요)
    public TMP_Text[] choiceLabels;         // 각 버튼 안의 텍스트

    [Header("스토리")]
    public TextAsset storyJson;             // story.json
    public StoryData story;

    [Header("미니게임 (에피소드 순서대로, Element 0 = EP.1)")]
    public GameObject[] minigames;
    [Tooltip("미니게임 칸이 비어 있을 때 대신 사용할 테스트 등급")]
    public Grade testGrade = Grade.A;

    [Header("엔딩 조건")]
    [Tooltip("A=2점, B=1점, F=0점.\nF가 하나라도 있으면 ENDING 02\n점수가 이 값 이상이면 ENDING 03\n나머지는 ENDING 01")]
    public int excellentScore = 7;

    [Header("설정")]
    [Tooltip("대사 속 'OO'을 이 이름으로 바꿔줌")]
    public string playerName = "OO";
    public bool italicNarration = true;
    [Min(0)] public int startEpisodeIndex = 0;  // 테스트용 (0 = EP.1)
    [Min(0.001f)] public float typingSpeed = 0.05f;

    [Header("게임이 완전히 끝났을 때 (타이틀로 가기 등)")]
    public UnityEvent onGameEnd;

    private enum Phase
    {
        Intro, Branch, AfterChoice, ResultIntro, Result, Outro,
        MidEnding, FinalIntro, FinalEnding
    }

    private Phase phase;
    private int episodeIndex;
    private DialogueLine[] currentLines;
    private int currentIndex;

    private bool isChoosing;
    private bool isWaitingMinigame;
    private bool isFinished;
    private bool isTyping;
    private Coroutine typingCoroutine;

    private Grade[] grades;
    private bool[] hasGrade;
    private int[] choicesMade;

    // ───────── 시작 ─────────

    private void Start()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

        if (minigames != null)
            foreach (GameObject mg in minigames)
                if (mg != null) mg.SetActive(false);

        // Inspector가 비어 있으면 JSON에서 바로 읽기
        if ((story == null || story.episodes == null || story.episodes.Length == 0) && storyJson != null)
            story = JsonUtility.FromJson<StoryData>(storyJson.text);

        if (story == null || story.episodes == null || story.episodes.Length == 0)
        {
            Debug.LogWarning("스토리가 비어 있어요. Story Json을 넣어주세요.");
            EndGame("스토리 없음");
            return;
        }

        int count = story.episodes.Length;
        grades = new Grade[count];
        hasGrade = new bool[count];
        choicesMade = new int[count];
        for (int i = 0; i < count; i++) choicesMade[i] = -1;

        StartEpisode(startEpisodeIndex);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            NextDialogue();
#else
        if (Input.GetKeyDown(KeyCode.Space))
            NextDialogue();
#endif
    }

    // ───────── 흐름 제어 ─────────

    private Episode CurrentEpisode =>
        episodeIndex < story.episodes.Length ? story.episodes[episodeIndex] : null;

    private void StartEpisode(int index)
    {
        if (index >= story.episodes.Length)
        {
            StartFinal();
            return;
        }

        episodeIndex = index;
        Debug.Log($"에피소드 시작: {CurrentEpisode.episodeName}");

        phase = Phase.Intro;
        PlayLines(CurrentEpisode.introLines);
    }

    private void StartFinal()
    {
        episodeIndex = story.episodes.Length;
        phase = Phase.FinalIntro;
        PlayLines(story.finalIntroLines);
    }

    private void PlayLines(DialogueLine[] lines)
    {
        currentLines = lines;
        currentIndex = 0;

        if (currentLines == null || currentLines.Length == 0)
        {
            OnLinesFinished();
            return;
        }

        dialoguePanel.SetActive(true);
        ShowLine();
    }

    // 대사 묶음 하나가 끝나면 다음 단계로
    private void OnLinesFinished()
    {
        Episode ep = CurrentEpisode;

        switch (phase)
        {
            case Phase.Intro:
                if (ep.choices != null && ep.choices.Length > 0)
                    ShowChoices(ep);
                else
                    PlayAfterChoice();
                break;

            case Phase.Branch:
                PlayAfterChoice();
                break;

            case Phase.AfterChoice:
                StartMinigame();
                break;

            case Phase.ResultIntro:
                phase = Phase.Result;
                PlayLines(GetResultLines(ep, grades[episodeIndex]));
                break;

            case Phase.Result:
                if (grades[episodeIndex] == Grade.F && ep.failEndsGame)
                {
                    phase = Phase.MidEnding;
                    PlayLines(ep.failEndingLines);
                }
                else
                {
                    phase = Phase.Outro;
                    PlayLines(ep.outroLines);
                }
                break;

            case Phase.Outro:
                StartEpisode(episodeIndex + 1);
                break;

            case Phase.MidEnding:
                EndGame("중도 엔딩");
                break;

            case Phase.FinalIntro:
                phase = Phase.FinalEnding;
                PlayLines(ChooseEnding());
                break;

            case Phase.FinalEnding:
                EndGame("최종 엔딩");
                break;
        }
    }

    private void PlayAfterChoice()
    {
        phase = Phase.AfterChoice;
        PlayLines(CurrentEpisode.afterChoiceLines);
    }

    // ───────── 대사 출력 ─────────

    private void ShowLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        DialogueLine line = currentLines[currentIndex];
        string speaker = ApplyTokens(line.speaker);
        string text = ApplyTokens(line.dialogue);

        bool isNarration = string.IsNullOrEmpty(speaker);
        nameText.text = speaker;
        if (namePanel != null) namePanel.SetActive(!isNarration);
        if (isNarration && italicNarration) text = "<i>" + text + "</i>";

        dialogueText.maxVisibleCharacters = 0;
        dialogueText.text = text;
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
                yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    public void NextDialogue()
    {
        if (isFinished || isChoosing || isWaitingMinigame)
            return;

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

        if (currentIndex >= currentLines.Length)
        {
            OnLinesFinished();
            return;
        }

        ShowLine();
    }

    // {grade} → 이번 에피소드 등급, {report} → 성적표, OO → 플레이어 이름
    private string ApplyTokens(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        if (s.Contains("{grade}"))
        {
            string g = (episodeIndex < hasGrade.Length && hasGrade[episodeIndex])
                ? grades[episodeIndex].ToString() : "-";
            s = s.Replace("{grade}", g);
        }

        if (s.Contains("{report}"))
            s = s.Replace("{report}", BuildReport());

        if (!string.IsNullOrEmpty(playerName) && playerName != "OO")
            s = s.Replace("OO", playerName);

        return s;
    }

    private string BuildReport()
    {
        List<string> parts = new List<string>();
        for (int i = 0; i < story.episodes.Length; i++)
        {
            if (story.episodes[i].hasMinigame && hasGrade[i])
                parts.Add($"{story.episodes[i].gradeName} {grades[i]}");
        }
        return string.Join(" / ", parts);
    }

    // ───────── 선택지 ─────────

    private void ShowChoices(Episode ep)
    {
        isChoosing = true;
        int count = ep.choices.Length;

        if (choiceButtons == null || count > choiceButtons.Length)
            Debug.LogWarning($"{ep.episodeName}: 선택지가 {count}개인데 버튼이 부족해요.");

        int buttonCount = choiceButtons != null ? choiceButtons.Length : 0;
        for (int i = 0; i < buttonCount; i++)
        {
            bool use = i < count;
            if (choiceButtons[i] != null) choiceButtons[i].SetActive(use);

            if (use && choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                choiceLabels[i].text = ApplyTokens(ep.choices[i].text);
        }

        if (choicePanel != null) choicePanel.SetActive(true);
    }

    // 버튼 OnClick에 연결: A 버튼 → 0, B 버튼 → 1, C 버튼 → 2
    public void SelectChoice(int choiceIndex)
    {
        if (!isChoosing) return;

        Episode ep = CurrentEpisode;
        if (choiceIndex < 0 || choiceIndex >= ep.choices.Length) return;

        if (choicePanel != null) choicePanel.SetActive(false);
        isChoosing = false;
        choicesMade[episodeIndex] = choiceIndex;

        phase = Phase.Branch;
        PlayLines(ep.choices[choiceIndex].lines);
    }

    // ───────── 미니게임 ─────────

    private void StartMinigame()
    {
        Episode ep = CurrentEpisode;

        if (!ep.hasMinigame)
        {
            phase = Phase.Outro;
            PlayLines(ep.outroLines);
            return;
        }

        isWaitingMinigame = true;
        dialoguePanel.SetActive(false);

        GameObject mg = GetMinigameObject(episodeIndex);
        if (mg != null)
        {
            mg.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{ep.episodeName}: 미니게임 오브젝트가 없어서 테스트 등급 {testGrade}(으)로 진행해요.");
            FinishMinigame(testGrade);
        }
    }

    // 미니게임이 끝나면 미니게임 스크립트에서 호출
    public void FinishMinigame(Grade grade)
    {
        if (!isWaitingMinigame) return;
        isWaitingMinigame = false;

        GameObject mg = GetMinigameObject(episodeIndex);
        if (mg != null) mg.SetActive(false);

        grades[episodeIndex] = grade;
        hasGrade[episodeIndex] = true;
        Debug.Log($"{CurrentEpisode.gradeName} 등급: {grade}");

        phase = Phase.ResultIntro;
        PlayLines(CurrentEpisode.resultIntroLines);
    }

    // 버튼 OnClick용: 0 = A, 1 = B, 2 = F
    public void FinishMinigameByIndex(int gradeIndex)
    {
        FinishMinigame((Grade)Mathf.Clamp(gradeIndex, 0, 2));
    }

    private GameObject GetMinigameObject(int index)
    {
        if (minigames == null || index >= minigames.Length) return null;
        return minigames[index];
    }

    private DialogueLine[] GetResultLines(Episode ep, Grade grade)
    {
        switch (grade)
        {
            case Grade.A: return ep.resultA;
            case Grade.B: return ep.resultB;
            default: return ep.resultF;
        }
    }

    // ───────── 엔딩 ─────────

    private DialogueLine[] ChooseEnding()
    {
        int score = 0;
        bool anyF = false;

        for (int i = 0; i < grades.Length; i++)
        {
            if (!hasGrade[i]) continue;
            if (grades[i] == Grade.F) anyF = true;
            score += grades[i] == Grade.A ? 2 : grades[i] == Grade.B ? 1 : 0;
        }

        Debug.Log($"최종 점수: {score}, F 있음: {anyF}");

        if (anyF) return story.endingRepeat;                    // ENDING 02
        if (score >= excellentScore) return story.endingExcellent; // ENDING 03
        return story.endingGraduate;                            // ENDING 01
    }

    private void EndGame(string reason)
    {
        isFinished = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        Debug.Log($"게임 종료: {reason}");
        onGameEnd?.Invoke();
    }

    // ───────── 다른 스크립트에서 쓸 수 있는 정보 ─────────

    // 해당 에피소드에서 고른 선택지 (0=A, 1=B, 2=C, 안 골랐으면 -1)
    public int GetChoice(int episode) =>
        choicesMade != null && episode < choicesMade.Length ? choicesMade[episode] : -1;

    // 지금 진행 중인 에피소드 번호 (0 = EP.1)
    public int CurrentEpisodeIndex => episodeIndex;

    // ───────── 에디터 전용: JSON → Inspector ─────────

#if UNITY_EDITOR
    [ContextMenu("스토리 JSON 불러오기")]
    private void ImportStoryJson()
    {
        if (storyJson == null)
        {
            Debug.LogWarning("Story Json 칸에 story.json을 먼저 넣어주세요.");
            return;
        }

        Undo.RecordObject(this, "Import Story JSON");
        story = JsonUtility.FromJson<StoryData>(storyJson.text);
        EditorUtility.SetDirty(this);

        Debug.Log($"스토리 불러오기 완료: 에피소드 {story.episodes.Length}개");
    }
#endif
}