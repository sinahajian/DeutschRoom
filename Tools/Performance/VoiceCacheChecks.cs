using System.Reflection;
using UnityEngine;

static class Checks
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Call(GermanVoice voice, string name) => typeof(GermanVoice).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(voice, null);
    static void Set(GermanVoice voice, string name, object value) => typeof(GermanVoice).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(voice, value);
    static T Get<T>(GermanVoice voice, string name) => (T)typeof(GermanVoice).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(voice)!;
    static GermanVoice Create()
    {
        var voice = new GermanVoice();
        Set(voice, "speaker", new AudioSource()); Set(voice, "presentation", new LessonPresentation());
        Call(voice, "OnEnable"); return voice;
    }
    static void Close(GermanVoice voice) => Call(voice, "OnDisable");
    static void Empty() => Check(Resources.Loaded.Count == 0, "Last narrator left cached assets behind");
    public static void Main()
    {
        for (int i = 0; i < 100; i++) Resources.Register("line " + i);
        Resources.Register("Nimm den Löffel.");
        Resources.Register("long success");Resources.Register("release");Resources.Register("next");
        Resources.BuildIndex();
        Resources.Index+="\n"+GermanVoice.Key("listed but missing");
        var a = Create();
        for (int i = 0; i < 100; i++) a.ClipFor("line " + i);
        Check(Resources.Loaded.Count == 12 && Resources.Unloads == 88, "Corpus scan did not respect 12-clip cap");
        int loads = Resources.Loads;
        var last = a.ClipFor("line 99");
        Check(Resources.Loads == loads, "Recent clip was loaded twice");
        a.ClipFor("line 0");
        Check(a.ClipFor("line 99") == last, "LRU did not retain recently used clip");
        Close(a); Empty();
        a=Create();Get<LessonPresentation>(a,"presentation").PlayVoice=false;
        int audioLoads=Resources.AudioLoads;
        a.Speak("line 1","line 2","line 3","line 4");
        Check(Resources.AudioLoads==audioLoads,"Muted/preflight narration decoded queued recordings");
        Close(a);Empty();

        a=Create();bool holding=true;
        a.SpeakContext(line=>line!="release"||holding,"release","next");
        Check(a.CurrentCaption=="release","Context warning never started");
        holding=false;Get<Coroutine>(a,"routine").Advance();
        Check(a.CurrentCaption=="next","Completed action did not interrupt stale warning immediately");
        a.Stop();holding=false;AudioSource.Played.Clear();
        a.SpeakContext(line=>line!="release"||holding,"long success","release","next");
        var queued=Get<Coroutine>(a,"routine");int attempts=0;
        while(queued.Advance() && attempts++<200) { }
        Check(attempts<200 && !AudioSource.Played.Any(p=>p.EndsWith(GermanVoice.Key("release"))),"Queued stale warning played after action completion");
        a.Speak("listed but missing");Check(a.NeedsTextFallback,"Indexed missing audio did not fall back to text");
        Close(a);Empty();

        a = Create(); var b = Create();
        a.Speak("line 1"); b.Speak("line 2");
        var first = Get<AudioSource>(a, "speaker").clip;
        var second = Get<AudioSource>(b, "speaker").clip;
        for (int i = 3; i < 100; i++) a.ClipFor("line " + i);
        Check(first != null && second != null && a.IsSpeaking && b.IsSpeaking, "Eviction invalidated an active narrator");
        a.Stop();
        Check(Get<AudioSource>(a, "speaker").clip == null && !a.IsSpeaking, "Stop retained its clip/source playback");
        Close(a); Check(second != null, "Disabling another narrator unloaded active audio");
        Close(b); Empty();

        Resources.Register("large A", 1024 * 1024); Resources.Register("large B", 1024 * 1024); Resources.Register("large C", 1024 * 1024);
        a = Create();
        a.ClipFor("large A"); a.ClipFor("large B"); a.ClipFor("large C");
        Check(Resources.Loaded.Count == 2, "8 MiB estimate did not constrain large clips");
        Resources.Register("oversize", 3 * 1024 * 1024);
        var large = a.ClipFor("oversize"); Check(large != null && Resources.Loaded.Count == 1, "Oversize requested clip was discarded");
        a.Stop(); Empty(); Close(a);

        a = Create(); a.Speak("line 1", "line 2");
        var routine = Get<Coroutine>(a, "routine");
        while (routine.Advance()) { }
        Check(a.CurrentCaption == "" && Get<AudioSource>(a, "speaker").clip == null, "Completed narration retained its clip/caption");
        a.Speak("missing recording"); Check(a.NeedsTextFallback, "Missing audio lost readable fallback");
        a.Stop(); Check(!a.NeedsTextFallback, "Fallback did not reset");
        Check(!a.IntroAudioPending, "Installed opening recording reported missing");
        loads = Resources.Loads;
        for (int i = 0; i < 100; i++) Check(!a.IntroAudioPending, "Opening status changed");
        Check(Resources.Loads == loads, "Opening status reloaded audio per UI query");
        Close(a); Empty();
        Console.WriteLine("PASS: actual GermanVoice; bounded LRU; active narrators; cleanup; metadata-only muted/preflight checks; queued/current stale warning removal; missing-audio fallback. Unity native memory/audio remain untested.");
    }
}
