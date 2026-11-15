using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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

    [Header("미니게임 선택지 보정")]
    [Min(0f)] public float runnerTimeChange = 5f;
    [Min(0f)] public float examTimeChange = 5f;
    [Min(1f)] public float hardMashDecayMultiplier = 1.15f;
    [Min(0f)] public float hardCardPreviewTime = 4f;
    [Min(1f)] public float hardTypingSpeedMultiplier = 1.25f;
    [Min(0f)] public float resultDisplayTime = 3f;

    [Header("설정")]
    [Tooltip("대사 속 'OO'을 이 이름으로 바꿔줌")]
    public string playerName = "OO";
    public bool italicNarration = true;
    [Min(0)] public int startEpisodeIndex = 0;  // 테스트용 (0 = EP.1)

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
    private int[] points;
    private int professorAffinity;
    private float examTimeBonus;
    private int[] choicesMade;
    private Scene minigameScene;
    private bool minigameConfigured;
    private readonly List<GameObject> pausedStoryRoots = new List<GameObject>();

    // ───────── 시작 ─────────

    private void Start()
    {
        if (choicePanel != null) choicePanel.SetActive(false);

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
        points = new int[count];
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
                else if (grades[episodeIndex] == Grade.F && episodeIndex == 3)
                {
                    phase = Phase.FinalEnding;
                    PlayLines(story.endingRepeat);
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

        int totalCharacters = dialogueText.textInfo.characterCount;
        isTyping = totalCharacters > 1 && GamePreferences.DialogueDelay > 0f;
        if (isTyping)
            typingCoroutine = StartCoroutine(TypeDialogue(totalCharacters));
        else
            dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator TypeDialogue(int totalCharacters)
    {
        int visible = 1;
        float elapsed = 0f;
        dialogueText.maxVisibleCharacters = visible;

        while (visible < totalCharacters)
        {
            yield return null;
            if (SettingsMenu.BlocksGameplay)
                continue;

            float delay = GamePreferences.DialogueDelay;
            if (delay <= 0f)
                break;

            elapsed += Time.deltaTime;
            while (elapsed >= delay && visible < totalCharacters)
            {
                elapsed -= delay;
                visible++;
            }

            dialogueText.maxVisibleCharacters = visible;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        typingCoroutine = null;
    }

    public void NextDialogue()
    {
        if (SettingsMenu.BlocksGameplay || isFinished || isChoosing || isWaitingMinigame || currentLines == null)
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
                ? $"{grades[episodeIndex]} ({points[episodeIndex]}점)" : "-";
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
                parts.Add($"{story.episodes[i].gradeName} {grades[i]} ({points[i]}점)");
        }
        return string.Join(" / ", parts) + $"\n총점 {TotalScore()} / 400 · 교수님 호감도 {professorAffinity} / 2";
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
        if (SettingsMenu.BlocksGameplay || !isChoosing) return;

        Episode ep = CurrentEpisode;
        if (choiceIndex < 0 || choiceIndex >= ep.choices.Length) return;

        if (choicePanel != null) choicePanel.SetActive(false);
        isChoosing = false;
        choicesMade[episodeIndex] = choiceIndex;

        if ((episodeIndex == 0 && choiceIndex == 2) || (episodeIndex == 3 && choiceIndex == 1))
            professorAffinity = Mathf.Min(2, professorAffinity + 1);
        if (episodeIndex == 1)
            examTimeBonus = choiceIndex == 0 ? -examTimeChange : examTimeChange;

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

        StartCoroutine(LoadMinigame("MiniGame" + (episodeIndex + 1)));
    }

    private IEnumerator LoadMinigame(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            StopForMinigameError($"빌드 목록에 {sceneName} 씬이 없습니다.");
            yield break;
        }

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root == transform.root.gameObject || !root.activeSelf)
                continue;

            pausedStoryRoots.Add(root);
            root.SetActive(false);
        }

        minigameConfigured = false;
        SceneManager.sceneLoaded += ConfigureMinigame;
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (load != null)
            yield return load;

        SceneManager.sceneLoaded -= ConfigureMinigame;
        if (!minigameConfigured)
        {
            if (minigameScene.IsValid() && minigameScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(minigameScene);

            RestoreStory();
            StopForMinigameError($"{sceneName}의 미니게임 매니저를 찾지 못했습니다.");
        }
    }

    // sceneLoaded는 미니게임 매니저의 Start 전에 호출된다.
    private void ConfigureMinigame(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Additive || scene.name != "MiniGame" + (episodeIndex + 1))
            return;

        minigameScene = scene;
        SceneManager.SetActiveScene(scene);
        int choice = GetChoice(episodeIndex);

        switch (episodeIndex)
        {
            case 0:
                RunnerPlayer runner = FindInScene<RunnerPlayer>(scene);
                if (runner == null) return;
                runner.maxLives = StoryRules.FullHearts;
                runner.playTime = Mathf.Max(1f, runner.playTime +
                    (choice == 0 ? runnerTimeChange : choice > 0 ? -runnerTimeChange : 0f));
                runner.sceneFlow = ConnectMinigame(runner, runner.sceneFlow);
                break;

            case 1:
                MashGameManager mash = FindInScene<MashGameManager>(scene);
                if (mash == null) return;
                if (choice == 0)
                    mash.decreasePerSecond *= hardMashDecayMultiplier;
                mash.sceneFlow = ConnectMinigame(mash, mash.sceneFlow);
                break;

            case 2:
                MemoryGameManager memory = FindInScene<MemoryGameManager>(scene);
                if (memory == null) return;
                float studyTimeChange = choice == 0 ? -examTimeChange : 0f;
                memory.timeLimit = Mathf.Max(1f, memory.timeLimit + examTimeBonus + studyTimeChange);
                if (choice == 1)
                    memory.previewTime = Mathf.Min(memory.previewTime, hardCardPreviewTime);
                memory.sceneFlow = ConnectMinigame(memory, memory.sceneFlow);
                break;

            case 3:
                TypingGameManager typing = FindInScene<TypingGameManager>(scene);
                if (typing == null) return;
                typing.maxLives = StoryRules.FullHearts;
                if (choice == 0)
                    typing.fallSpeed *= hardTypingSpeedMultiplier;
                typing.sceneFlow = ConnectMinigame(typing, typing.sceneFlow);
                break;

            default:
                return;
        }

        minigameConfigured = true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>();
            if (component != null) return component;
        }

        return null;
    }

    private MiniGameSceneFlow ConnectMinigame(Component manager, MiniGameSceneFlow flow)
    {
        if (flow == null)
            flow = manager.gameObject.AddComponent<MiniGameSceneFlow>();

        flow.delay = resultDisplayTime;
        flow.ReturnToStory = hearts => StartCoroutine(ReturnFromMinigame(hearts));
        return flow;
    }

    private IEnumerator ReturnFromMinigame(int hearts)
    {
        SceneManager.SetActiveScene(gameObject.scene);
        yield return SceneManager.UnloadSceneAsync(minigameScene);
        RestoreStory();
        isWaitingMinigame = false;

        grades[episodeIndex] = StoryRules.GradeForHearts(hearts);
        points[episodeIndex] = StoryRules.PointsForHearts(hearts);
        hasGrade[episodeIndex] = true;

        phase = Phase.ResultIntro;
        PlayLines(CurrentEpisode.resultIntroLines);
    }

    private void RestoreStory()
    {
        SceneManager.SetActiveScene(gameObject.scene);
        foreach (GameObject root in pausedStoryRoots)
            if (root != null) root.SetActive(true);
        pausedStoryRoots.Clear();
    }

    private void StopForMinigameError(string message)
    {
        Debug.LogError(message, this);
        dialoguePanel.SetActive(true);
        nameText.text = "";
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.text = message;
        isFinished = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= ConfigureMinigame;
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

    private int TotalScore()
    {
        int total = 0;
        foreach (int score in points)
            total += score;
        return total;
    }

    private DialogueLine[] ChooseEnding()
    {
        switch (StoryRules.FinalEnding(TotalScore(), professorAffinity))
        {
            case 2: return story.endingRepeat;
            case 3: return story.endingExcellent;
            default: return story.endingGraduate;
        }
    }

    private void EndGame(string reason)
    {
        isFinished = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        Debug.Log($"게임 종료: {reason}");
        onGameEnd?.Invoke();
        if (gameObject.scene.name == "Story_S")
            SceneManager.LoadScene("Start_S1");
    }

    // ───────── 다른 스크립트에서 쓸 수 있는 정보 ─────────

    // 해당 에피소드에서 고른 선택지 (0=A, 1=B, 2=C, 안 골랐으면 -1)
    public int GetChoice(int episode) =>
        choicesMade != null && episode >= 0 && episode < choicesMade.Length ? choicesMade[episode] : -1;

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