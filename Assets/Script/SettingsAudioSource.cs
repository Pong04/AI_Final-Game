using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class SettingsAudioSource : MonoBehaviour
{
    public enum Channel { BackgroundMusic, SoundEffects }

    [SerializeField] private Channel channel;
    private AudioSource source;
    private float originalVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        originalVolume = source.volume;
    }

    private void OnEnable()
    {
        GamePreferences.AudioChanged += ApplyVolume;
        ApplyVolume();
    }

    private void OnDisable()
    {
        GamePreferences.AudioChanged -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        float volume = channel == Channel.BackgroundMusic ? GamePreferences.Music : GamePreferences.Effects;
        source.volume = originalVolume * volume;
    }
}
