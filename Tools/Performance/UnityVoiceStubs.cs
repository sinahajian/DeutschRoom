// Managed test doubles: exercise the actual narrator/cache code, not Unity native allocation/audio.
using System.Collections;
namespace UnityEngine
{
    public class Object
    {
        public bool unloaded;
        public static bool operator ==(Object a, Object b)
        {
            bool aNull = ReferenceEquals(a, null) || a.unloaded;
            bool bNull = ReferenceEquals(b, null) || b.unloaded;
            return aNull || bNull ? aNull == bNull : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public class MonoBehaviour : Object
    {
        public readonly GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : new() => gameObject.AddComponent<T>();
        public Coroutine StartCoroutine(IEnumerator body)
        { var routine = new Coroutine { body = body }; routine.Advance(); return routine; }
        public void StopCoroutine(Coroutine routine) => routine.stopped = true;
    }
    public sealed class GameObject { public T AddComponent<T>() where T : new() => new T(); }
    public sealed class Coroutine
    {
        public IEnumerator body;
        public bool stopped;
        public bool Advance() { Time.unscaledTime+=.05f; return !stopped && body.MoveNext(); }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public sealed class AudioSource : Object
    {
        public bool isPlaying, playOnAwake;
        public float volume, spatialBlend;
        public AudioClip clip;
        public static readonly List<string> Played = new List<string>();
        public void Play() { if (clip == null) throw new Exception("Playing an unloaded clip"); isPlaying = true;Played.Add(clip.path); }
        public void Stop() => isPlaying = false;
    }
    public sealed class AudioClip : Object
    {
        public string path;
        public int samples, channels = 1;
        public float length => samples / 24000f;
    }
    public sealed class WaitForSecondsRealtime { public WaitForSecondsRealtime(float time) { } }
    public sealed class TextAsset : Object { public string text; }
    public static class Time { public static float unscaledTime; }
    public static class Mathf { public static float Clamp(float x, float min, float max) => Math.Clamp(x, min, max); }
    public static class Resources
    {
        private static readonly Dictionary<string, int> files = new Dictionary<string, int>();
        public static readonly Dictionary<string, AudioClip> Loaded = new Dictionary<string, AudioClip>();
        public static int Loads, Unloads, AudioLoads;
        public static string Index;
        public static void BuildIndex() => Index=string.Join("\n",files.Keys.Select(p=>p.Split('/')[1]));
        public static void Register(string phrase, int samples = 24000) => files["VoiceIntroA11/" + GermanVoice.Key(phrase)] = samples;
        public static T Load<T>(string path) where T : Object
        {
            Loads++;
            if(path=="VoiceIntroA11Index")return Index==null?null:(T)(Object)new TextAsset {text=Index};
            AudioLoads++;
            if (!files.TryGetValue(path, out int samples)) return null;
            if (!Loaded.TryGetValue(path, out var clip)) Loaded[path] = clip = new AudioClip { path = path, samples = samples };
            return (T)(Object)clip;
        }
        public static void UnloadAsset(Object asset)
        {
            if (asset is not AudioClip clip || clip.unloaded) throw new Exception("Double/invalid unload");
            clip.unloaded = true; Loaded.Remove(clip.path); Unloads++;
        }
    }
}
public sealed class LessonPresentation : UnityEngine.MonoBehaviour
{
    public bool PlayVoice = true;
    public float VoiceVolume = 1;
}
