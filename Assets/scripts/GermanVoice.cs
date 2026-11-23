using System.Collections;
using System.Collections.Generic;
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
    public bool NeedsTextFallback { get; private set; }
    private bool testPlaying;
    private bool? introAudioPending;
    private System.Func<string,bool> relevant;
    private static bool indexChecked;
    private static HashSet<string> available;
    // Shared across narrators so evicting an idle recording cannot interrupt another voice.
    private const int CacheLimit = 12;
    private const long CacheByteLimit = 8 * 1024 * 1024;
    private sealed class CachedClip
    {
        public string key;
        public AudioClip clip;
        public long bytes;
    }
    private static readonly Dictionary<string, LinkedListNode<CachedClip>> clips = new Dictionary<string, LinkedListNode<CachedClip>>();
    private static readonly LinkedList<CachedClip> recent = new LinkedList<CachedClip>();
    private static readonly List<GermanVoice> voices = new List<GermanVoice>();
    private static long cachedBytes;
    private void OnEnable() => voices.Add(this);
    private void OnDisable()
    {
        Stop();
        voices.Remove(this);
        if (voices.Count == 0)
        {
            foreach (var entry in recent) if (entry.clip != null) Resources.UnloadAsset(entry.clip);
            recent.Clear(); clips.Clear(); cachedBytes = 0;
        }
    }

    private static bool InUse(AudioClip clip)
    {
        foreach (var voice in voices)
            if (voice != null && voice.speaker != null && voice.speaker.clip == clip) return true;
        return false;
    }

    private static void TrimCache(AudioClip requested = null)
    {
        var node = recent.First;
        // A single large requested clip, or simultaneously active clips, may exceed the budget.
        while (node != null && (clips.Count > CacheLimit || cachedBytes > CacheByteLimit))
        {
            var next = node.Next;
            var entry = node.Value;
            if (entry.clip != requested && !InUse(entry.clip))
            {
                clips.Remove(entry.key); recent.Remove(node); cachedBytes -= entry.bytes;
                if (entry.clip != null) Resources.UnloadAsset(entry.clip);
            }
            node = next;
        }
    }
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
    public void SpeakContext(System.Func<string,bool> isRelevant, params string[] lines) { Begin(false,lines,false,isRelevant); }
    public void SpeakIdentification(params string[] lines) { Begin(false, lines, true); }
    public void PlayTest()
    {
        Begin(true, new[] { "Nimm den Löffel." });
    }
    public bool IntroAudioPending
    {
        get
        {
            if (!introAudioPending.HasValue) introAudioPending = !HasRecording("Nimm den Löffel.");
            return introAudioPending.Value;
        }
    }
    private bool HasRecording(string text)
    {
        if(!indexChecked)
        {
            indexChecked=true;
            var asset=Resources.Load<TextAsset>("VoiceIntroA11Index");
            if(asset!=null)
            {
                available=new HashSet<string>(System.StringComparer.Ordinal);
                using(var reader=new System.IO.StringReader(asset.text))
                {string key;while((key=reader.ReadLine())!=null)if(key.Length==8)available.Add(key);}
            }
        }
        return available!=null ? available.Contains(Key(text)) : ClipFor(text)!=null;
    }
    // Borrowed recording: active narrator sources pin their clips; idle clips can be evicted.
    public AudioClip ClipFor(string text)
    {
        string key = Key(text);
        if (clips.TryGetValue(key, out var node))
        {
            if (node.Value.clip != null)
            { recent.Remove(node); recent.AddLast(node); return node.Value.clip; }
            recent.Remove(node); clips.Remove(key); cachedBytes -= node.Value.bytes;
        }
        AudioClip clip = Resources.Load<AudioClip>("VoiceIntroA11/" + key);
        if (clip == null) return null;
        // Conservative decoded PCM estimate; import format and recording quality remain unchanged.
        long bytes = (long)clip.samples * clip.channels * sizeof(float);
        node = recent.AddLast(new CachedClip { key = key, clip = clip, bytes = bytes });
        clips.Add(key, node); cachedBytes += bytes;
        TrimCache(clip);
        return clip;
    }
    private void Begin(bool test, string[] lines, bool identification = false, System.Func<string,bool> isRelevant = null)
    {
        if (speaker == null) { speaker = GetComponent<AudioSource>(); speaker.playOnAwake = false; speaker.spatialBlend = 0; }
        Stop();
        relevant=isRelevant;
        IsIdentification = identification;
        testPlaying = test;
        LastRequest = lines != null ? (string[])lines.Clone() : new string[0];
        // Keep the new course on its own voice pack; any missing recording needs readable guidance.
        NeedsTextFallback = System.Array.Exists(LastRequest, line =>
            !string.IsNullOrEmpty(line) && (relevant==null || relevant(line)) && !HasRecording(line));
        routine = StartCoroutine(Play(LastRequest));
    }
    private IEnumerator Play(string[] lines)
    {
        foreach (string text in lines)
        {
            if (string.IsNullOrEmpty(text) || relevant!=null && !relevant(text)) continue;
            CurrentCaption = text;
            bool audible = testPlaying || presentation.PlayVoice;
            AudioClip clip = audible ? ClipFor(text) : null;
            if(audible && clip==null)NeedsTextFallback=true;
            float duration = Mathf.Clamp(text.Length * .045f + 1, 2.5f, 12);
            if (clip != null)
            {
                speaker.volume = presentation.VoiceVolume;
                speaker.clip = clip;
                speaker.Play();
                duration = clip.length;
            }
            if(relevant==null)yield return new WaitForSecondsRealtime(duration+.15f);
            else
            {
                float end=Time.unscaledTime+duration+.15f;
                while(Time.unscaledTime<end && relevant(text))yield return null;
                if(!relevant(text))speaker.Stop();
            }
            speaker.clip = null;
            TrimCache();
        }
        CurrentCaption = "";
        testPlaying = false;
        routine = null;
        relevant=null;
    }
    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        relevant=null;
        CurrentCaption = "";
        testPlaying = false;
        NeedsTextFallback = false;
        if (speaker != null) { speaker.Stop(); speaker.clip = null; }
        TrimCache();
    }
}
