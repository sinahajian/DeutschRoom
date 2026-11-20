using System;
using System.Collections.Generic;
using UnityEngine;

// Local profile: preview -> vocabulary search -> article sorting on the table.
public sealed class LearningProgress : MonoBehaviour
{
    private const string SaveKey = "DeutschRoom.Learning.v1";
    [Serializable] private sealed class ItemPose
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public bool parentLocal;
        public string parentPath;
        public Vector3 worldPosition;
        public Quaternion worldRotation;
    }
    [Serializable] private sealed class SaveData
    {
        public bool started;
        public int stage;
        public int pickupCount = 17;
        public int fixedCount;
        public bool tablePrepared;
        public List<ItemPose> poses = new List<ItemPose>();
    }
    private readonly string[] articles = { "der", "die", "das" };
    private SaveData data = new SaveData();
    private PickupItem[] items;
    private FixedLearningTarget[] fixedTargets;
    public int SearchCount => items == null ? 0 : items.Length + (fixedTargets == null ? 0 : fixedTargets.Length);
    private Collider table;
    private PlayerInteraction player;
    public event Action TaskChanged;
    public int TotalStages => items == null ? 0 : SearchCount + articles.Length;
    public bool PersistenceEnabled { get; set; } = true;
    public bool IsPreview => !data.started;
    public int Stage => data.stage;
    public bool IsComplete => items != null && data.stage >= SearchCount + articles.Length;
    private string Article => articles[Mathf.Clamp(data.stage - SearchCount, 0, articles.Length - 1)];

    private void Start()
    {
        player = GetComponent<PlayerInteraction>();
        items = FindObjectsByType<PickupItem>();
        Array.Sort(items, (a, b) => { int order = a.LessonOrder.CompareTo(b.LessonOrder); return order != 0 ? order : string.CompareOrdinal(Id(a), Id(b)); });
        // Start with the spoon, then introduce the other available words.
        int spoon = Array.FindIndex(items, item => item.DisplayName == "der Löffel");
        if (spoon > 0) { PickupItem first = items[0]; items[0] = items[spoon]; items[spoon] = first; }
        fixedTargets = FindObjectsByType<FixedLearningTarget>();
        Array.Sort(fixedTargets, (a, b) => a.Order.CompareTo(b.Order));
        GameObject tabletop = GameObject.Find("TableTop");
        if (tabletop != null) table = tabletop.GetComponent<Collider>();
        try
        {
            string saved = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(saved)) data = JsonUtility.FromJson<SaveData>(saved) ?? new SaveData();
        }
        catch (Exception) { data = new SaveData(); }
        // Preserve the article-sorting/completion stage of saves from the original 17-item room.
        data.stage = RestoreStage(data.stage, data.pickupCount, items.Length, data.fixedCount, fixedTargets.Length);
        data.pickupCount = items.Length;
        data.fixedCount = fixedTargets.Length;
        if (data.poses == null) data.poses = new List<ItemPose>();
        foreach (ItemPose pose in data.poses)
        {
            PickupItem item = Array.Find(items, candidate => Id(candidate) == pose.id);
            if (item == null) continue;
            Transform anchor = pose.parentLocal && !string.IsNullOrEmpty(pose.parentPath) ? GameObject.Find(pose.parentPath)?.transform : null;
            if (anchor != null)
            {
                item.transform.SetParent(anchor, false);
                item.transform.localPosition = pose.position;
                item.transform.localRotation = pose.rotation;
            }
            else
            {
                // A drawer item placed on a table must not reattach to the drawer on reload.
                if (item.GetComponentInParent<Openable>() != null) item.transform.SetParent(null, true);
                item.transform.SetPositionAndRotation(pose.parentLocal ? pose.worldPosition : pose.position,
                    pose.parentLocal ? pose.worldRotation : pose.rotation);
            }
        }
        Physics.SyncTransforms();
    }

    public static int RestoreStage(int stage, int savedPickupCount, int currentPickupCount) =>
        RestoreStage(stage, savedPickupCount, currentPickupCount, 0, 0);
    public static int RestoreStage(int stage, int savedPickupCount, int currentPickupCount, int savedFixedCount, int currentFixedCount)
    {
        int previousPickups = savedPickupCount > 0 ? savedPickupCount : 17;
        int previousSearch = previousPickups + Mathf.Max(0, savedFixedCount);
        int currentSearch = currentPickupCount + currentFixedCount;
        if (stage >= previousSearch) stage += Mathf.Max(0, currentSearch - previousSearch);
        else if (stage >= previousPickups) stage += Mathf.Max(0, currentPickupCount - previousPickups);
        return Mathf.Clamp(stage, 0, currentSearch + 3);
    }

    private static string Id(PickupItem item) => item.gameObject.scene.path + "/" + item.name;

    public static void ClearSavedProgress() { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); }

    public bool IsArticleTask => items != null && !IsPreview && !IsComplete && data.stage >= SearchCount;
    public bool IsClearingTable => IsArticleTask && !data.tablePrepared;
    public string CurrentArticle => IsArticleTask ? Article : "";
    public string TaskSpeech => items == null || IsPreview ? "Willkommen! Tippe auf einen Punkt und höre den Namen." :
        IsComplete ? "Geschafft! Alle Aufgaben abgeschlossen." :
        data.stage < items.Length ? GermanLessonSpeech.Find(items[data.stage].DisplayName) :
        data.stage < SearchCount ? GermanLessonSpeech.Find(fixedTargets[data.stage - items.Length].Item.DisplayName) :
        !data.tablePrepared ? GermanLessonSpeech.ClearTable :
        "Lege " + RequiredCount + " Gegenstände mit dem Artikel " + Article + " auf den Tisch.";

    private PickupItem[] OnTableItems => items == null ? new PickupItem[0] : Array.FindAll(items, IsOnTable);
    private bool Matches(PickupItem item) => item.DisplayName.StartsWith(Article + " ", StringComparison.Ordinal);
    private PickupItem[] WrongTableItems => Array.FindAll(OnTableItems, item => !Matches(item));

    public Collider MapTable => table;
    public bool TryGetMapTarget(out Vector3 position, out LessonMinimap.TargetKind kind)
    {
        position = default;
        kind = LessonMinimap.TargetKind.Find;
        if (items == null || IsPreview || IsComplete) return false;
        if (!IsArticleTask)
        { position = data.stage < items.Length ? items[data.stage].transform.position : fixedTargets[data.stage - items.Length].transform.position; return true; }
        if (player.HeldItem != null)
        {
            if (data.tablePrepared && Matches(player.HeldItem))
            { position = table != null ? table.bounds.center : player.transform.position; kind = LessonMinimap.TargetKind.Table; }
            else
            {
                position = table != null ? new Vector3(table.bounds.max.x + .9f, 0, table.bounds.center.z - .8f) : player.transform.position;
                kind = LessonMinimap.TargetKind.PutAway;
            }
            return table != null;
        }
        PickupItem[] remove = data.tablePrepared ? WrongTableItems : OnTableItems;
        if (remove.Length > 0) { position = remove[0].transform.position; kind = LessonMinimap.TargetKind.Remove; return true; }
        if (!data.tablePrepared) return false;
        PickupItem nearest = null;
        float distance = float.PositiveInfinity;
        foreach (PickupItem item in items)
        {
            if (item.IsHeld || !Matches(item) || IsOnTable(item)) continue;
            float candidate = (item.transform.position - player.transform.position).sqrMagnitude;
            if (candidate < distance) { nearest = item; distance = candidate; }
        }
        if (nearest == null) return false;
        position = nearest.transform.position;
        return true;
    }

    private List<string> Guidance()
    {
        List<string> lines = new List<string> { TaskSpeech };
        if (!IsArticleTask)
        {
            return lines;
        }
        PickupItem[] remove = data.tablePrepared ? WrongTableItems : OnTableItems;
        if (remove.Length > 0) lines.Add(GermanLessonSpeech.Remove(remove[0].DisplayName));
        else if (!data.tablePrepared && player.HeldItem != null) lines.Add(GermanLessonSpeech.PutElsewhere);
        else if (data.tablePrepared) lines.Add(GermanLessonSpeech.Count(TableCount));
        return lines;
    }

    private void SpeakWithGuidance(params string[] opening)
    {
        List<string> lines = new List<string>(opening);
        foreach (string line in Guidance()) if (!lines.Contains(line)) lines.Add(line);
        player.Voice.Speak(lines.ToArray());
    }

    private bool PrepareEmptyTable()
    {
        if (!IsArticleTask || data.tablePrepared || OnTableItems.Length > 0 || player.HeldItem != null) return false;
        data.tablePrepared = true;
        return true;
    }

    public void RepeatTask()
    {
        if (player == null) return;
        if (PrepareEmptyTable()) Save();
        if (IsComplete) SpeakWithGuidance();
        else SpeakWithGuidance(GermanLessonSpeech.Stage(data.stage + 1));
    }

    public void StartLessons()
    {
        if (items == null || data.started) return;
        data.started = true;
        Save();
        RepeatTask();
        TaskChanged?.Invoke();
    }

    public void Found(PickupItem item, bool takenFromTable = false)
    {
        if (items == null || IsPreview || IsComplete) return;
        if (IsArticleTask)
        {
            if (!takenFromTable) return;
            if (!data.tablePrepared) SpeakWithGuidance("Vom Tisch genommen.", GermanLessonSpeech.PutElsewhere);
            else SpeakWithGuidance("Vom Tisch genommen.", GermanLessonSpeech.Count(TableCount));
            return;
        }
        if (data.stage >= items.Length) { SpeakWithGuidance("Lege den Gegenstand zuerst ab."); return; }
        if (items[data.stage] == item)
        {
            data.stage++;
            data.tablePrepared = false;
            PrepareEmptyTable();
            TaskChanged?.Invoke();
            Save();
            SpeakWithGuidance(GermanLessonSpeech.Found(item.DisplayName), GermanLessonSpeech.Stage(data.stage + 1));
        }
        else SpeakWithGuidance("Versuch es noch einmal.", "Lege den Gegenstand zuerst ab.");
    }

    public bool Identified(KitchenInteractable item)
    {
        if (items == null || IsPreview || IsComplete || data.stage < items.Length || data.stage >= SearchCount ||
            item == null) return false;
        KitchenInteractable target = fixedTargets[data.stage - items.Length].Item;
        if (target != item && !item.transform.IsChildOf(target.transform)) return false;
        item = target;
        data.stage++;
        data.tablePrepared = false;
        PrepareEmptyTable();
        TaskChanged?.Invoke();
        Save();
        SpeakWithGuidance(GermanLessonSpeech.FixedFound(item.DisplayName), GermanLessonSpeech.Stage(data.stage + 1));
        return true;
    }

    public bool IsOnTable(PickupItem item)
    {
        if (table == null || item == null || item.IsHeld) return false;
        Bounds bounds = item.BoundsAt(item.transform.position, item.transform.rotation);
        Bounds top = table.bounds;
        return bounds.min.x >= top.min.x - .02f && bounds.max.x <= top.max.x + .02f &&
               bounds.min.z >= top.min.z - .02f && bounds.max.z <= top.max.z + .02f &&
               Mathf.Abs(bounds.min.y - top.max.y) < .08f;
    }

    public int RequiredCount => items == null ? 0 : Mathf.Min(10, Array.FindAll(items, Matches).Length);
    public int TableCount => items == null ? 0 : Array.FindAll(items, item => Matches(item) && IsOnTable(item)).Length;

    private void AdvanceArticle(List<string> lines)
    {
        data.stage++;
        data.tablePrepared = false;
        PrepareEmptyTable();
        TaskChanged?.Invoke();
        lines.Add(GermanLessonSpeech.Finished);
        if (!IsComplete) lines.Add(GermanLessonSpeech.Stage(data.stage + 1));
        lines.AddRange(Guidance());
    }

    public void Placed(PickupItem item)
    {
        if (items == null) return;
        if (IsArticleTask)
        {
            bool onTable = IsOnTable(item);
            List<string> lines = new List<string>();
            if (onTable) lines.Add(GermanLessonSpeech.Placed(item.DisplayName));
            if (!data.tablePrepared)
            {
                if (PrepareEmptyTable()) lines.Add(GermanLessonSpeech.TableReady);
                lines.AddRange(Guidance());
            }
            else if (TableCount >= RequiredCount && RequiredCount > 0 && WrongTableItems.Length == 0)
                AdvanceArticle(lines);
            else if (onTable && !Matches(item))
            {
                lines.Add("Für diese Aufgabe brauchen wir den Artikel " + Article + ".");
                lines.Add(GermanLessonSpeech.Remove(item.DisplayName));
            }
            else
            {
                lines.Add(GermanLessonSpeech.Count(TableCount));
                PickupItem[] wrong = WrongTableItems;
                if (wrong.Length > 0) lines.Add(GermanLessonSpeech.Remove(wrong[0].DisplayName));
                else if (!onTable) lines.Add(TaskSpeech);
            }
            player.Voice.Speak(lines.ToArray());
        }
        Save();
    }

    private static string TransformPath(Transform anchor)
    {
        string path = anchor.name;
        while (anchor.parent != null) { anchor = anchor.parent; path = anchor.name + "/" + path; }
        return "/" + path;
    }

    private void Save()
    {
        data.poses.Clear();
        foreach (PickupItem item in items)
        {
            if (item.IsHeld) continue;
            Transform anchor = item.transform.parent;
            bool local = anchor != null && anchor.GetComponentInParent<Openable>() != null;
            data.poses.Add(new ItemPose {
                id = Id(item), parentLocal = local, parentPath = local ? TransformPath(anchor) : null,
                position = local ? item.transform.localPosition : item.transform.position,
                rotation = local ? item.transform.localRotation : item.transform.rotation,
                worldPosition = item.transform.position, worldRotation = item.transform.rotation
            });
        }
        if (!PersistenceEnabled) return;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

}
