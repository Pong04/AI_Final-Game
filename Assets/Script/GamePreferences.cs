using System;
using UnityEngine;

public static class GamePreferences
{
    private const string Prefix = "Graduation.Settings.";
    private static readonly float[] DialogueDelays = { 0.09f, 0.05f, 0.025f, 0f };

    public static event Action AudioChanged;

    public static float Master => GetVolume("Master");
    public static float Music => GetVolume("Music");
    public static float Effects => GetVolume("Effects");
    public static int DialogueSpeed => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "DialogueSpeed", 1), 0, 3);
    public static float DialogueDelay => DialogueDelays[DialogueSpeed];
    public static bool Fullscreen => PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;

    private static float GetVolume(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + key, 1f));

    public static void SetVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(Prefix + key, Mathf.Clamp01(value));
        AudioListener.volume = Master;
        AudioChanged?.Invoke();
    }

    public static void SetDialogueSpeed(int index)
    {
        PlayerPrefs.SetInt(Prefix + "DialogueSpeed", Mathf.Clamp(index, 0, 3));
        PlayerPrefs.Save();
    }

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(Prefix + "Fullscreen", fullscreen ? 1 : 0);
        ApplyDisplay();
        PlayerPrefs.Save();
    }

    public static void ApplyDisplay()
    {
        if (Application.isEditor)
            return;

        Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }
}
