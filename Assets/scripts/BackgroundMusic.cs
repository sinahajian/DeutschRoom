using UnityEngine;

// Keep music on its own source so muting it never mutes the lesson voice.
public sealed class BackgroundMusic : MonoBehaviour
{
    private const string PreferenceKey = "DeutschRoom.MusicEnabled";
    private AudioSource source;
    private GermanVoice voice;
    public bool MusicEnabled { get; private set; }

    private void Start()
    {
        voice = GetComponent<GermanVoice>();
        GameObject child = new GameObject("Background music");
        child.transform.SetParent(transform, false);
        source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0;
        source.volume = .18f;
        source.clip = Resources.Load<AudioClip>("Music/background");
        MusicEnabled = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
        if (MusicEnabled && source.clip != null) source.Play();
    }

    public void Toggle()
    {
        MusicEnabled = !MusicEnabled;
        PlayerPrefs.SetInt(PreferenceKey, MusicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        if (source == null || source.clip == null) return;
        if (MusicEnabled)
        {
            if (source.time > 0) source.UnPause();
            else source.Play();
        }
        else source.Pause();
    }

    private void Update()
    {
        if (source == null) return;
        float target = voice != null && voice.IsSpeaking ? .045f : .18f;
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .5f);
    }

    private void OnDestroy()
    {
        if (source != null) { source.Stop(); Destroy(source.gameObject); }
    }
}
