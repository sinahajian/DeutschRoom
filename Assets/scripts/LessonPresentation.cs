using UnityEngine;

public sealed class LessonPresentation : MonoBehaviour
{
    public enum OutputMode { Text, Voice, Both }
    private const string ModeKey = "DeutschRoom.OutputMode";
    private const string VolumeKey = "DeutschRoom.VoiceVolume";
    public OutputMode Mode { get; private set; }
    public float VoiceVolume { get; private set; }
    public bool DeviceMuted { get; private set; }
    public bool ReportedNoSound { get; private set; }
    public bool PersistenceEnabled { get; set; } = true;
    public bool WantsVoice => Mode != OutputMode.Text;
    public bool AudioUnavailable => DeviceMuted || ReportedNoSound || VoiceVolume <= .001f || AudioListener.volume <= .001f || AudioListener.pause;
    public bool ShowText => Mode != OutputMode.Voice || AudioUnavailable;
    public bool PlayVoice => WantsVoice && !AudioUnavailable;
    public bool ShowAudioWarning => WantsVoice && AudioUnavailable;
    public string WarningText => "Kein Ton: Prüfe Medienlautstärke und Lautlosmodus. Die Aufgaben werden als Text angezeigt.";

    private void Awake()
    {
        Mode = (OutputMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, (int)OutputMode.Both), 0, 2);
        VoiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1));
        RefreshDeviceAudio();
    }
    private void OnEnable() { AudioSettings.Mobile.OnMuteStateChanged += OnDeviceMute; }
    private void OnDisable() { AudioSettings.Mobile.OnMuteStateChanged -= OnDeviceMute; Save(); }
    private void OnDeviceMute(bool muted) { DeviceMuted = muted; }
    public void RefreshDeviceAudio()
    {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        DeviceMuted = AudioSettings.Mobile.muteState;
#else
        DeviceMuted = false;
#endif
    }
    private void OnApplicationFocus(bool focused) { if (focused) RefreshDeviceAudio(); }
    private void OnApplicationPause(bool paused) { if (paused) Save(); else RefreshDeviceAudio(); }
    public void SetMode(OutputMode mode)
    {
        Mode = (OutputMode)Mathf.Clamp((int)mode, 0, 2);
        if (PersistenceEnabled) PlayerPrefs.SetInt(ModeKey, (int)Mode);
    }
    public void SetVoiceVolume(float volume)
    {
        VoiceVolume = Mathf.Clamp01(volume);
        if (PersistenceEnabled) PlayerPrefs.SetFloat(VolumeKey, VoiceVolume);
    }
    public void ReportNoSound(bool noSound) { ReportedNoSound = noSound; }
    public void Save() { if (PersistenceEnabled) PlayerPrefs.Save(); }
}
