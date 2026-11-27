using System;
using System.Collections.Generic;
using UnityEngine;

public enum SupportLanguage { Persian, English, Arabic, Ukrainian }

public static class SupportLanguages
{
    private const string LanguageKey = "DeutschRoom.SupportLanguage.v1";
    private const string BilingualKey = "DeutschRoom.BilingualInstructions.v1";
    private const string CheckpointKey = "DeutschRoom.LanguageCheckpoint.v1";
    [Serializable] private sealed class Entry { public string source = "", en = "", fa = "", ar = "", uk = ""; }
    [Serializable] private sealed class Catalog { public Entry[] entries = Array.Empty<Entry>(); }
    private static Dictionary<string, Entry> entries;
    private static Font supportFont;
    public static readonly string[] Names = { "فارسی", "English", "العربية", "Українська" };
    public static bool HasSelection => PlayerPrefs.HasKey(LanguageKey);
    public static SupportLanguage Selected => (SupportLanguage)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 3);
    public static bool IsRightToLeft => Selected == SupportLanguage.Persian || Selected == SupportLanguage.Arabic;
    public static bool Bilingual => PlayerPrefs.GetInt(BilingualKey, 1) != 0;
    public static int AcknowledgedCheckpoint => PlayerPrefs.GetInt(CheckpointKey, 0);
    public static void Select(SupportLanguage language, int completed)
    {
        if (!HasSelection) PlayerPrefs.SetInt(CheckpointKey, SupportLanguageRules.Checkpoint(completed));
        PlayerPrefs.SetInt(LanguageKey, (int)language);
        PlayerPrefs.Save();
    }
    public static void SetBilingual(bool enabled)
    {
        PlayerPrefs.SetInt(BilingualKey, enabled ? 1 : 0); PlayerPrefs.Save();
    }
    public static void Acknowledge(int completed, bool bilingual)
    {
        PlayerPrefs.SetInt(CheckpointKey, SupportLanguageRules.Checkpoint(completed));
        SetBilingual(bilingual);
    }
    public static void ResetCheckpoints()
    {
        PlayerPrefs.SetInt(CheckpointKey, 0); PlayerPrefs.Save();
    }
    public static string Text(string source) => Text(source, Selected);
    public static string Text(string source, SupportLanguage language)
    {
        if (string.IsNullOrEmpty(source) || language == SupportLanguage.English) return source ?? "";
        if (entries == null)
        {
            entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var asset = Resources.Load<TextAsset>("Localization/SupportLanguages");
            if (asset != null)
                foreach (var entry in JsonUtility.FromJson<Catalog>(asset.text).entries)
                    entries[entry.source] = entry;
        }
        if (!entries.TryGetValue(source, out var found)) return source;
        return language == SupportLanguage.Persian ? found.fa : language == SupportLanguage.Arabic ? found.ar : found.uk;
    }
    public static Font FontFor(SupportLanguage language)
    {
        if (supportFont == null) supportFont = Resources.Load<Font>("Localization/Fonts/DeutschRoomSupport");
        return supportFont;
    }
}
