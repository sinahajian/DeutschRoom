// Only the persistence/resource/font-measurement boundary is stubbed. Tests execute
// production preference/checkpoint/lookup/shaping code; these are not render tests.
using System.Text.Json;
namespace UnityEngine
{
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string,int> values=new();
        public static bool HasKey(string key)=>values.ContainsKey(key);
        public static int GetInt(string key,int fallback=0)=>values.GetValueOrDefault(key,fallback);
        public static void SetInt(string key,int value)=>values[key]=value;
        public static void Save() { }
    }
    public static class Mathf { public static int Clamp(int value,int lo,int hi)=>Math.Clamp(value,lo,hi); }
    public sealed class Font { public string name="TestFont"; }
    public sealed class TextAsset { public string text; }
    public static class Resources
    {
        public static T Load<T>(string path) where T:class
        {
            if (typeof(T)==typeof(Font)) return new Font() as T;
            return new TextAsset {text=File.ReadAllText("Assets/Resources/"+path+".json")} as T;
        }
    }
    public static class JsonUtility
    {
        public static T FromJson<T>(string text)=>JsonSerializer.Deserialize<T>(text,new JsonSerializerOptions {IncludeFields=true});
    }
    public sealed class GUIContent { public string text;public GUIContent(string value) {text=value;} }
    public struct Vector2 { public float x,y; }
    public sealed class GUIStyle
    {
        public Font font;public int fontSize=16;
        public Vector2 CalcSize(GUIContent value)=>new Vector2 {x=value.text.Length*8,y=20};
    }
}
