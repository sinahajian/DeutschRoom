using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class GermanVoice : MonoBehaviour
{
    private AudioSource speaker;
    private Coroutine routine;
    private LessonPresentation presentation;
    public bool IsSpeaking => speaker != null && speaker.isPlaying && speaker.volume > .001f;
    public string[] LastRequest { get; private set; } = new string[0];
    public string CurrentCaption { get; private set; } = "";
    public bool IsIdentification { get; private set; }
    private bool testPlaying;
    private void Awake()
    {
        presentation = GetComponent<LessonPresentation>();
        if (presentation == null) presentation = gameObject.AddComponent<LessonPresentation>();
    }
    private void Update()
    {
        if (speaker == null) return;
        speaker.volume = presentation.VoiceVolume;
        if (!testPlaying && !presentation.PlayVoice && speaker.isPlaying) speaker.Stop();
    }
    public static string Key(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text) hash = unchecked((hash ^ c) * 16777619);
        return hash.ToString("x8");
    }
    public void Speak(params string[] lines) { Begin(false, lines); }
    public void SpeakIdentification(params string[] lines) { Begin(false, lines, true); }
    public void PlayTest()
    {
        Begin(true, new[] { "Du hast den Löffel gefunden. Jetzt kannst du ihn ablegen." });
    }
    private void Begin(bool test, string[] lines, bool identification = false)
    {
        if (speaker == null) { speaker = GetComponent<AudioSource>(); speaker.playOnAwake = false; speaker.spatialBlend = 0; }
        Stop();
        IsIdentification = identification;
        testPlaying = test;
        LastRequest = lines != null ? (string[])lines.Clone() : new string[0];
        routine = StartCoroutine(Play(LastRequest));
    }
    private IEnumerator Play(string[] lines)
    {
        foreach (string text in lines)
        {
            if (string.IsNullOrEmpty(text)) continue;
            CurrentCaption = text;
            bool audible = testPlaying || presentation.PlayVoice;
            AudioClip clip = audible ? Resources.Load<AudioClip>("Voice/" + Key(text)) : null;
            float duration = Mathf.Clamp(text.Length * .045f + 1, 2.5f, 12);
            if (clip != null)
            {
                speaker.volume = presentation.VoiceVolume;
                speaker.clip = clip;
                speaker.Play();
                duration = clip.length;
            }
            else if (audible) Debug.LogWarning("Missing German voice: " + text);
            yield return new WaitForSecondsRealtime(duration + .15f);
        }
        CurrentCaption = "";
        testPlaying = false;
        routine = null;
    }
    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        CurrentCaption = "";
        testPlaying = false;
        if (speaker != null) speaker.Stop();
    }
}
