using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(-200)]
public class SettingsMenu : MonoBehaviour
{
    private enum Page { Closed, Pause, Settings, Controls, ConfirmTitle }

    private static readonly Color PanelColor = new Color32(25, 31, 46, 255);
    private static readonly Color ButtonColor = new Color32(50, 68, 97, 255);
    private static readonly Color AccentColor = new Color32(110, 197, 200, 255);
    private static readonly string[] SpeedNames = { "느림", "보통", "빠름", "즉시" };

    private static SettingsMenu instance;
    private Page page;
    private Canvas canvas;
    private GameObject overlay;
    private RectTransform content;
    private Button titleButton;
    private bool titleButtonAvailable;
    private TMP_FontAsset font;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool loadingTitle;
    private int blockedThroughFrame = -1;
    private GameObject previousSelection;
    private TMP_InputField previousInput;
    private Coroutine focusRestore;
    private Coroutine preview;
    private const string PreviewText = "교수님, 집에 가고 싶어요...";

    public static bool BlocksGameplay => instance != null &&
        (instance.page != Page.Closed || instance.loadingTitle || Time.frameCount <= instance.blockedThroughFrame);
    public static bool IsPaused => instance != null && instance.page != Page.Closed;
    private static bool IsTitle => SceneManager.GetActiveScene().name == "Start_S1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("SettingsMenu", typeof(RectTransform));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SettingsMenu>();
        instance.BuildCanvas();
        AudioListener.volume = GamePreferences.Master;
        GamePreferences.ApplyDisplay();
    }

    public static void ShowTitleButton(TMP_FontAsset titleFont)
    {
        if (instance == null)
            return;

        instance.font = titleFont;
        instance.titleButtonAvailable = true;
        if (instance.titleButton == null)
        {
            instance.titleButton = instance.CreateButton(instance.canvas.transform, "환경설정",
                () => instance.Open(Page.Settings));
            RectTransform rect = (RectTransform)instance.titleButton.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 85f);
            rect.sizeDelta = new Vector2(280f, 66f);
        }

        instance.titleButton.gameObject.SetActive(instance.page == Page.Closed);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (page != Page.Closed)
            RestoreGame();
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        loadingTitle = false;
        titleButtonAvailable = false;
        if (page != Page.Closed)
            Close(restoreFocus: false);

        if (titleButton != null)
            titleButton.gameObject.SetActive(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool escape = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!escape || loadingTitle)
            return;

        switch (page)
        {
            case Page.Closed:
                Open(IsTitle ? Page.Settings : Page.Pause);
                break;
            case Page.Pause:
                Close();
                break;
            case Page.Settings:
                if (IsTitle) Close();
                else Render(Page.Pause);
                break;
            case Page.Controls:
                Render(Page.Settings);
                break;
            case Page.ConfirmTitle:
                Render(Page.Pause);
                break;
        }
    }

    private void Open(Page firstPage)
    {
        if (page != Page.Closed || loadingTitle)
            return;

        if (focusRestore != null)
        {
            StopCoroutine(focusRestore);
            focusRestore = null;
        }

        EventSystem events = EventSystem.current;
        previousSelection = events != null ? events.currentSelectedGameObject : null;
        previousInput = previousSelection != null ? previousSelection.GetComponent<TMP_InputField>() : null;
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        Time.timeScale = 0f;
        AudioListener.pause = previousAudioPause || !IsTitle;
        blockedThroughFrame = Time.frameCount + 1;
        Render(firstPage);
    }

    private void Close(bool restoreFocus = true)
    {
        if (page == Page.Closed)
            return;

        EventSystem.current?.SetSelectedGameObject(null);
        RestoreGame();
        page = Page.Closed;
        overlay.SetActive(false);
        if (titleButton != null)
            titleButton.gameObject.SetActive(IsTitle && titleButtonAvailable);
        blockedThroughFrame = Time.frameCount + 1;
        PlayerPrefs.Save();

        if (restoreFocus)
            focusRestore = StartCoroutine(RestoreFocus());
        else
        {
            previousSelection = null;
            previousInput = null;
        }
    }

    private IEnumerator RestoreFocus()
    {
        while (BlocksGameplay)
            yield return null;

        if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(previousSelection);
            if (previousInput != null && previousInput.IsInteractable())
                previousInput.ActivateInputField();
        }

        previousSelection = null;
        previousInput = null;
        focusRestore = null;
    }

    private void RestoreGame()
    {
        Time.timeScale = previousTimeScale;
        AudioListener.pause = previousAudioPause;
    }

    private void ReturnToTitle()
    {
        loadingTitle = true;
        Close(restoreFocus: false);
        SceneManager.LoadScene("Start_S1");
    }

    private void BuildCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referenceResolution = new Vector2(960f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform shade = CreateRect("Modal", canvas.transform);
        shade.anchorMin = Vector2.zero;
        shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        shade.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        overlay = shade.gameObject;
        overlay.SetActive(false);

        RectTransform panel = CreateRect("Panel", shade);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = panel.anchorMin;
        panel.sizeDelta = new Vector2(740f, 810f);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        content = CreateRect("Content", panel);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(38f, 32f);
        content.offsetMax = new Vector2(-38f, -32f);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
    }

    private void Render(Page nextPage)
    {
        page = nextPage;
        if (titleButton != null)
            titleButton.gameObject.SetActive(false);

        if (preview != null)
        {
            StopCoroutine(preview);
            preview = null;
        }

        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (font == null)
        {
            foreach (TMP_Text sceneText in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (sceneText.font != null && sceneText.font.HasCharacter('환', true, true))
                {
                    font = sceneText.font;
                    break;
                }
            }
        }

        overlay.SetActive(true);

        Selectable first;
        switch (page)
        {
            case Page.Pause:
                AddHeading("일시정지");
                first = CreateButton(content, "계속하기", () => Close());
                CreateButton(content, "환경설정", () => Render(Page.Settings));
                if (Application.CanStreamedLevelBeLoaded("Start_S1"))
                    CreateButton(content, "타이틀로", () => Render(Page.ConfirmTitle));
                break;

            case Page.Settings:
                first = RenderSettings();
                break;

            case Page.Controls:
                AddHeading("조작 안내");
                AddLabel("대사 진행   Space 또는 대사창 클릭", 50f);
                AddLabel("EP.1 점프   Space", 50f);
                AddLabel("EP.2 연타   Space", 50f);
                AddLabel("EP.3 카드 뒤집기   마우스 클릭", 50f);
                AddLabel("EP.4 단어 제출   Enter 또는 Space", 50f);
                AddLabel("일시정지 및 뒤로   Esc", 50f);
                first = CreateButton(content, "뒤로", () => Render(Page.Settings));
                break;

            default:
                AddHeading("타이틀로 돌아갈까요?");
                AddLabel("현재 플레이 진행 상황이 사라집니다.", 65f);
                first = CreateButton(content, "취소", () => Render(Page.Pause));
                CreateButton(content, "타이틀로 돌아가기", ReturnToTitle);
                break;
        }

        SetMenuNavigation();
        EventSystem.current?.SetSelectedGameObject(first.gameObject);
    }

    private void SetMenuNavigation()
    {
        Selectable previous = null;
        foreach (Selectable item in content.GetComponentsInChildren<Selectable>())
        {
            if (!item.IsInteractable())
                continue;

            Navigation navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = previous };
            item.navigation = navigation;
            if (previous != null)
            {
                Navigation previousNavigation = previous.navigation;
                previousNavigation.selectOnDown = item;
                previous.navigation = previousNavigation;
            }

            previous = item;
        }
    }

    private Selectable RenderSettings()
    {
        AddHeading("환경설정");
        Slider first = AddVolume("전체 음량", "Master", GamePreferences.Master);
        AddVolume("배경음", "Music", GamePreferences.Music);
        AddVolume("효과음", "Effects", GamePreferences.Effects);

        TMP_Text sample = CreateText(content, "", 28f);
        sample.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        TMP_Text speedCaption = null;
        Button speed = CreateButton(content, $"대사 속도 · {SpeedNames[GamePreferences.DialogueSpeed]}", () =>
        {
            GamePreferences.SetDialogueSpeed((GamePreferences.DialogueSpeed + 1) % SpeedNames.Length);
            speedCaption.text = $"대사 속도 · {SpeedNames[GamePreferences.DialogueSpeed]}";
            PlayPreview(sample);
        });
        speedCaption = speed.GetComponentInChildren<TMP_Text>();
        sample.transform.SetSiblingIndex(speed.transform.GetSiblingIndex() + 1);
        PlayPreview(sample);

        TMP_Text screenCaption = null;
        Button screen = CreateButton(content, DisplayLabel(), () =>
        {
            GamePreferences.SetFullscreen(!GamePreferences.Fullscreen);
            screenCaption.text = DisplayLabel();
        });
        screenCaption = screen.GetComponentInChildren<TMP_Text>();
        screen.interactable = !Application.isEditor;
        CreateButton(content, "조작 안내", () => Render(Page.Controls));
        CreateButton(content, "뒤로", () =>
        {
            if (IsTitle) Close();
            else Render(Page.Pause);
        });

        return first;
    }

    private static string DisplayLabel()
    {
        if (Application.isEditor) return "화면 모드 · 빌드에서 변경 가능";
        return GamePreferences.Fullscreen ? "화면 모드 · 전체 화면" : "화면 모드 · 창 모드";
    }

    private void PlayPreview(TMP_Text sample)
    {
        if (preview != null)
            StopCoroutine(preview);

        sample.text = PreviewText;
        preview = StartCoroutine(AnimatePreview(sample));
    }

    private static IEnumerator AnimatePreview(TMP_Text sample)
    {
        float delay = GamePreferences.DialogueDelay;
        if (delay <= 0f)
        {
            sample.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        for (int visible = 1; visible <= sample.text.Length; visible++)
        {
            sample.maxVisibleCharacters = visible;
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private Slider AddVolume(string label, string key, float value)
    {
        RectTransform row = CreateRect(label, content);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        TMP_Text caption = CreateText(row, label, 28f);
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.gameObject.AddComponent<LayoutElement>().preferredWidth = 155f;

        RectTransform bar = CreateRect("Slider", row);
        LayoutElement barLayout = bar.gameObject.AddComponent<LayoutElement>();
        barLayout.flexibleWidth = 1f;
        barLayout.preferredHeight = 42f;
        bar.gameObject.AddComponent<Image>().color = Color.clear;

        RectTransform track = CreateRect("Track", bar);
        track.anchorMin = new Vector2(0f, 0.37f);
        track.anchorMax = new Vector2(1f, 0.63f);
        track.offsetMin = track.offsetMax = Vector2.zero;
        track.gameObject.AddComponent<Image>().color = ButtonColor;

        RectTransform handleArea = CreateRect("Handle Area", bar);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(24f, 0f);
        Image knob = handle.gameObject.AddComponent<Image>();
        knob.color = AccentColor;

        Slider slider = bar.gameObject.AddComponent<Slider>();
        slider.targetGraphic = knob;
        slider.handleRect = handle;
        slider.value = value;

        TMP_Text percentage = CreateText(row, $"{Mathf.RoundToInt(value * 100f)}%", 26f);
        percentage.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        slider.onValueChanged.AddListener(level =>
        {
            percentage.text = $"{Mathf.RoundToInt(level * 100f)}%";
            GamePreferences.SetVolume(key, level);
        });
        return slider;
    }

    private void AddHeading(string text) => AddLabel(text, 72f, 44f);

    private void AddLabel(string text, float height, float size = 30f)
    {
        CreateText(content, text, size).gameObject.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = CreateRect(label, parent);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = ButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
        colors.selectedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.colorMultiplier = 1.2f;
        button.colors = colors;
        button.onClick.AddListener(click);

        RectTransform textRect = CreateText(rect, label, 30f).rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        return button;
    }

    private TMP_Text CreateText(Transform parent, string label, float size)
    {
        TMP_Text text = CreateRect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = label;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
