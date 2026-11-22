using System;
using System.Collections.Generic;
using UnityEngine;

// Local profile: preview -> vocabulary search -> article sorting on the table.
public sealed class LearningProgress : MonoBehaviour
{
    private const string SaveKey = "DeutschRoom.Learning.v1";
    private const string IntroSaveKey = "DeutschRoom.Learning.IntroA11.v1";
    private const string CourseKey = "DeutschRoom.Learning.SelectedCourse";
    public static bool IntroSelected => PlayerPrefs.GetInt(CourseKey, 1) == 1;
    public static void SelectCourse(bool intro) { PlayerPrefs.SetInt(CourseKey, intro ? 1 : 0); PlayerPrefs.Save(); }
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
        public List<string> completedTasks = new List<string>();
        public List<ItemPose> poses = new List<ItemPose>();
    }
    private readonly string[] articles = { "der", "die", "das" };
    private SaveData data = new SaveData();
    private PickupItem[] items;
    private PickupItem[] allItems;
    private IntroLessonCatalog introCatalog;
    private KitchenInteractable[] introTargets;
    private PickupItem introHeldAtActivation;
    private uint introCarryAtActivation;
    private bool reviewHintRequested;
    public bool IsIntroCourse => introCatalog != null;
    private bool grammarIdentified, grammarPicked;
    private uint grammarPickupSequence, grammarPlacementAtActivation;
    private LessonPlacementScenario placementScenario;
    private LessonScenarioSession scenarioSession;
    public bool IsScenarioTask => GrammarTask != null && GrammarTask.IsScenario;
    public LessonScenarioSession ScenarioSession => scenarioSession;
    public bool ScenarioNeedsConfirmation => IsScenarioTask && scenarioSession != null && scenarioSession.NeedsConfirmation;
    public void ConfirmScenario() { if (ScenarioNeedsConfirmation) scenarioSession.Confirm(CurrentTaskId); }
    public bool CanScenarioPickup(PickupItem item) => !IsScenarioTask || scenarioSession == null || scenarioSession.CanPickup(item);
    public bool IsPlacementTask => GrammarTask != null && GrammarTask.RequiresPlacement;
    public LessonPlacementScenario PlacementScenario => placementScenario;
    public bool IsPlacementReference(PickupItem item) => item != null && ((IsPlacementTask && placementScenario != null && item == placementScenario.Reference) || (IsScenarioTask && scenarioSession != null && scenarioSession.IsFixed(item)));
    public string GrammarAnswerHelp => string.IsNullOrEmpty(GrammarTask?.answerHelp) ? "Wähle jetzt den passenden Artikel." : GrammarTask.answerHelp;
    public string GrammarFeedback { get; private set; } = "";
    public IntroLessonCatalog.Task GrammarTask => IsIntroCourse && !IsPreview && !IsComplete && introCatalog.tasks[data.stage].IsGrammar ? introCatalog.tasks[data.stage] : null;
    public bool GrammarAnswerOpen
    {
        get
        {
            var task = GrammarTask;
            if (task == null) return false;
            if (task.IsScenario) return scenarioSession != null && scenarioSession.IsReady;
            var target = introTargets[data.stage] as PickupItem;
            bool freshPlacement = task.RequiresPlacement && grammarPicked && target != null &&
                target.PlacementSequence != grammarPlacementAtActivation && target.LastPlacedCarrySequence == grammarPickupSequence &&
                placementScenario != null && placementScenario.ContainsTarget();
            bool freshPickup = player.HeldItem != null && player.HeldItem == target && target.IsHeld &&
                (target != introHeldAtActivation || target.CarrySequence != introCarryAtActivation);
            return task.ActionSatisfied(freshPickup, grammarIdentified, freshPlacement);
        }
    }
    public void SubmitGrammarAnswer(string taskId, int choice)
    {
        if (!GrammarAnswerOpen || GrammarTask.id != taskId) return;
        if (!GrammarTask.AcceptsChoice(choice))
        {
            GrammarFeedback = string.IsNullOrEmpty(GrammarTask.answerHelp) ? "Das ist noch nicht richtig. Versuche einen anderen Artikel." :
                "Das ist noch nicht richtig. Prüfe die Form und versuche es erneut.";
            player.Voice.Speak(GrammarFeedback, GrammarTask.question);
            return;
        }
        CompleteIntroTask();
    }
    private string ActiveSaveKey => IsIntroCourse ? IntroSaveKey : SaveKey;
    public string CourseName => IsIntroCourse ? "A1.1–C1.2 · " + TotalStages + " Aufgaben" : "Bisherige Aufgaben · " + TotalStages;
    public bool IsReviewTask => IsIntroCourse && !IsPreview && !IsComplete && introCatalog.tasks[data.stage].IsReview;
    public bool ReviewHintRequested => reviewHintRequested;
    public string CurrentTaskId => IsIntroCourse && !IsComplete ? introCatalog.tasks[data.stage].id : "";
    private FixedLearningTarget[] fixedTargets;
    public int SearchCount => IsIntroCourse ? introCatalog.tasks.Length :
        items == null ? 0 : items.Length + (fixedTargets == null ? 0 : fixedTargets.Length);
    private Collider table;
    private PlayerInteraction player;
    public event Action TaskChanged;
    public int TotalStages => items == null ? 0 : SearchCount + (IsIntroCourse ? 0 : articles.Length);
    public bool PersistenceEnabled { get; set; } = true;
    public bool IsPreview => !data.started;
    public int Stage => data.stage;
    public bool IsComplete => items != null && data.stage >= TotalStages;
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
        allItems = items;
        if (IntroSelected)
        {
            introCatalog = IntroLessonCatalog.Load();
            introTargets = introCatalog.Resolve(allItems, fixedTargets);
        }
        GameObject tabletop = GameObject.Find("TableTop");
        if (tabletop != null) table = tabletop.GetComponent<Collider>();
        try
        {
            string saved = PlayerPrefs.GetString(ActiveSaveKey, "");
            if (!string.IsNullOrEmpty(saved)) data = JsonUtility.FromJson<SaveData>(saved) ?? new SaveData();
        }
        catch (Exception) { data = new SaveData(); }
        // Preserve the article-sorting/completion stage of saves from the original 17-item room.
        if (data.completedTasks == null) data.completedTasks = new List<string>();
        data.stage = IsIntroCourse ? introCatalog.ResumeStage(data.completedTasks) :
            RestoreStage(data.stage, data.pickupCount, items.Length, data.fixedCount, fixedTargets.Length);
        data.pickupCount = items.Length;
        data.fixedCount = fixedTargets.Length;
        if (data.poses == null) data.poses = new List<ItemPose>();
        foreach (ItemPose pose in data.poses)
        {
            PickupItem item = Array.Find(allItems, candidate => Id(candidate) == pose.id);
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
        CaptureIntroActivation();
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

    // Restart only the selected course; the other profile is never erased.
    public static void ClearSavedProgress() { PlayerPrefs.DeleteKey(IntroSelected ? IntroSaveKey : SaveKey); PlayerPrefs.Save(); }

    public bool IsArticleTask => !IsIntroCourse && items != null && !IsPreview && !IsComplete && data.stage >= SearchCount;
    public bool IsClearingTable => IsArticleTask && !data.tablePrepared;
    public string CurrentArticle => IsArticleTask ? Article : "";
    public string TaskSpeech => items == null || IsPreview ? (IsIntroCourse ? GermanLessonSpeech.IntroWelcome : "Willkommen! Tippe auf einen Punkt und höre den Namen.") :
        IsComplete ? (IsIntroCourse ? GermanLessonSpeech.IntroComplete(TotalStages) : "Geschafft! Alle Aufgaben abgeschlossen.") :
        IsIntroCourse ? introCatalog.tasks[data.stage].instruction :
        data.stage < items.Length ? GermanLessonSpeech.Find(items[data.stage].DisplayName) :
        data.stage < SearchCount ? GermanLessonSpeech.Find(fixedTargets[data.stage - items.Length].Item.DisplayName) :
        !data.tablePrepared ? GermanLessonSpeech.ClearTable :
        "Lege " + RequiredCount + " Gegenstände mit dem Artikel " + Article + " auf den Tisch.";

    private IntroLessonCatalog translationCatalog;
    public string TaskEnglish
    {
        get
        {
            if (items == null || IsPreview) return "Welcome! Start the course and discover the objects.";
            if (IsComplete) return "Well done! You have completed all " + TotalStages + " tasks. You can keep practising freely.";
            if (IsIntroCourse) return introCatalog.tasks[data.stage].englishInstruction;
            if (IsArticleTask) return !data.tablePrepared ? "Before we begin, clear the table."
                : "Place " + RequiredCount + " objects with the German article '" + Article + "' on the table.";
            if (translationCatalog == null) translationCatalog = IntroLessonCatalog.Load();
            string target = data.stage < items.Length ? items[data.stage].name : fixedTargets[data.stage - items.Length].Item.name;
            foreach (var task in translationCatalog.tasks)
                if (!task.IsReview && task.targetName == target) return task.englishInstruction;
            return "English translation is unavailable for this task.";
        }
    }

    private PickupItem[] OnTableItems => items == null ? new PickupItem[0] : Array.FindAll(items, IsOnTable);
    private bool Matches(PickupItem item) => item.DisplayName.StartsWith(Article + " ", StringComparison.Ordinal);
    private PickupItem[] WrongTableItems => Array.FindAll(OnTableItems, item => !Matches(item));

    public Collider MapTable => table;
    public bool TryGetMapTarget(out Vector3 position, out LessonMinimap.TargetKind kind)
    {
        position = default;
        kind = LessonMinimap.TargetKind.Find;
        if (items == null || IsPreview || IsComplete) return false;
        if (IsIntroCourse)
        {
            if (IsScenarioTask) return scenarioSession != null && scenarioSession.TryGetTarget(out position);
            if (IsReviewTask && !reviewHintRequested) return false;
            if (IsPlacementTask && player.HeldItem == introTargets[data.stage] && placementScenario != null)
            { position = placementScenario.ZonePosition; kind = LessonMinimap.TargetKind.Table; return true; }
            position = introTargets[data.stage].transform.position;
            return true;
        }
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
        if (IsScenarioTask && scenarioSession != null)
            return scenarioSession.CaseReaderOpen ? new List<string> { TaskSpeech, scenarioSession.CurrentCasePage } : GrammarAnswerOpen ? new List<string> { GrammarAnswerHelp, GrammarTask.question } : new List<string> { TaskSpeech, scenarioSession.Status };
        List<string> lines = new List<string> { TaskSpeech };
        if (!IsArticleTask)
        {
            if (IsIntroCourse && data.stage == 0 && player.HeldItem == null)
                lines.Insert(0, GermanLessonSpeech.PickupHelp);
            if (IsIntroCourse && !IsComplete)
            {
                bool pickup = introCatalog.tasks[data.stage].RequiresPickup;
                if (GrammarAnswerOpen) return new List<string> { GrammarAnswerHelp, GrammarTask.question };
                if (IsPlacementTask && player.HeldItem == introTargets[data.stage])
                    return new List<string> { TaskSpeech, "Lege den Gegenstand vollständig in die markierte Zone auf dem Tisch." };
                if (player.HeldItem != null)
                    lines.Insert(0, pickup ? GermanLessonSpeech.ReleaseBeforeNext : GermanLessonSpeech.ReleaseOptional);
                if (!pickup && (data.stage == 0 || introCatalog.tasks[data.stage - 1].RequiresPickup))
                    lines.Insert(0, GermanLessonSpeech.IdentifyHelp);
            }
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
        CaptureIntroActivation();
        Save();
        RepeatTask();
        TaskChanged?.Invoke();
    }

    public void Found(PickupItem item, bool takenFromTable = false)
    {
        if (items == null || IsPreview || IsComplete) return;
        if (IsIntroCourse)
        {
            if (IsScenarioTask) return;
            if (item == null || !item.IsHeld || player.HeldItem != item) return;
            if (item == introHeldAtActivation && item.CarrySequence == introCarryAtActivation) return;
            if ((introCatalog.tasks[data.stage].RequiresPickup || IsPlacementTask) && introTargets[data.stage] == item)
            {
                if (IsPlacementTask)
                {
                    grammarPicked = true; grammarPickupSequence = item.CarrySequence;
                    player.Voice.Speak("Lege den Gegenstand vollständig in die markierte Zone auf dem Tisch.");
                }
                else if (GrammarTask != null) player.Voice.Speak(GrammarAnswerHelp, GrammarTask.question);
                else CompleteIntroTask();
            }
            else SpeakWithGuidance((introCatalog.tasks[data.stage].RequiresPickup || IsPlacementTask) ? GermanLessonSpeech.WrongPickup : GermanLessonSpeech.IdentifyHelp);
            return;
        }
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
            string success = GermanLessonSpeech.Found(item.DisplayName);
            data.stage++;
            data.tablePrepared = false;
            PrepareEmptyTable();
            TaskChanged?.Invoke();
            Save();
            if (IsComplete) SpeakWithGuidance(success);
            else SpeakWithGuidance(success, GermanLessonSpeech.Stage(data.stage + 1));
        }
        else SpeakWithGuidance("Versuch es noch einmal.", "Lege den Gegenstand zuerst ab.");
    }

    public bool Identified(KitchenInteractable item)
    {
        if (IsIntroCourse)
        {
            if (IsPreview || IsComplete || item == null || introCatalog.tasks[data.stage].InteractionKind != "I") return false;
            KitchenInteractable expected = introTargets[data.stage];
            if (item != expected && !item.transform.IsChildOf(expected.transform)) return false;
            FixedLearningTarget identifiedOwner = item.GetComponentInParent<FixedLearningTarget>();
            if (identifiedOwner != null && identifiedOwner.Item != expected) return false;
            if (GrammarTask != null)
            {
                grammarIdentified = true;
                player.Voice.Speak(GrammarAnswerHelp, GrammarTask.question);
            }
            else CompleteIntroTask();
            return true;
        }
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

    private void CompleteIntroTask()
    {
        var task = introCatalog.tasks[data.stage];
        if (!data.completedTasks.Contains(task.id)) data.completedTasks.Add(task.id);
        data.stage = introCatalog.ResumeStage(data.completedTasks);
        CaptureIntroActivation();
        TaskChanged?.Invoke();
        Save();
        if (IsComplete) SpeakWithGuidance(task.success);
        else SpeakWithGuidance(task.success, GermanLessonSpeech.Stage(data.stage + 1));
    }

    private void CaptureIntroActivation()
    {
        GrammarFeedback = "";
        grammarIdentified = grammarPicked = false;
        grammarPickupSequence = 0;
        reviewHintRequested = false;
        if (IsPlacementTask)
        {
            if (placementScenario == null) placementScenario = gameObject.AddComponent<LessonPlacementScenario>();
            placementScenario.Prepare(GrammarTask, allItems, table, player);
            grammarPlacementAtActivation = ((PickupItem)introTargets[data.stage]).PlacementSequence;
        }
        else if (placementScenario != null) placementScenario.Finish(player);
        if (IsScenarioTask)
        {
            if (scenarioSession == null) scenarioSession = gameObject.AddComponent<LessonScenarioSession>();
            scenarioSession.Prepare(GrammarTask, allItems, table, player, this);
        }
        else if (scenarioSession != null) scenarioSession.Finish();
        introHeldAtActivation = player != null ? player.HeldItem : null;
        introCarryAtActivation = introHeldAtActivation != null ? introHeldAtActivation.CarrySequence : 0;
    }

    public void ResetPlacementPractice()
    {
        if (!IsPlacementTask && !IsScenarioTask) return;
        bool scenario = IsScenarioTask;
        CaptureIntroActivation(); Save();
        player.Voice.Speak(scenario ? "Die Übung wurde neu vorbereitet." : "Die Tischübung wurde neu angeordnet.", TaskSpeech);
    }

    public void RevealReviewTarget()
    {
        if (!IsReviewTask || reviewHintRequested) return;
        reviewHintRequested = true;
        player.Voice.Speak(GermanLessonSpeech.ReviewHint, TaskSpeech);
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
        if (IsScenarioTask) { Save(); return; }
        if (IsPlacementTask)
        {
            if (item == introTargets[data.stage])
            {
                if (GrammarAnswerOpen) player.Voice.Speak(GrammarAnswerHelp, GrammarTask.question);
                else player.Voice.Speak("Lege den Gegenstand vollständig in die markierte Zone auf dem Tisch.", TaskSpeech);
            }
            Save(); return;
        }
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
        foreach (PickupItem item in allItems)
        {
            LessonPlacementScenario.OriginalPose original = null;
            bool temporary = placementScenario != null && placementScenario.TryGetOriginal(item, out original);
            if (!temporary && scenarioSession != null) temporary = scenarioSession.TryGetOriginal(item, out original);
            if (item.IsHeld && !temporary) continue;
            Transform anchor = temporary ? original.parent : item.transform.parent;
            Vector3 worldPosition = temporary ? original.position : item.transform.position;
            Quaternion worldRotation = temporary ? original.rotation : item.transform.rotation;
            bool local = anchor != null && anchor.GetComponentInParent<Openable>() != null;
            data.poses.Add(new ItemPose {
                id = Id(item), parentLocal = local, parentPath = local ? TransformPath(anchor) : null,
                position = local ? (temporary ? original.localPosition : item.transform.localPosition) : worldPosition,
                rotation = local ? (temporary ? original.localRotation : item.transform.localRotation) : worldRotation,
                worldPosition = worldPosition, worldRotation = worldRotation
            });
        }
        if (!PersistenceEnabled) return;
        PlayerPrefs.SetString(ActiveSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

}
