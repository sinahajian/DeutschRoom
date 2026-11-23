using System;
using System.Collections.Generic;
using UnityEngine;

// Local profile for the authored A1.1–C1.2 curriculum.
public sealed class LearningProgress : MonoBehaviour
{
    private const string IntroSaveKey = "DeutschRoom.Learning.IntroA11.v1";
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
        public List<string> completedTasks = new List<string>();
        public List<ItemPose> poses = new List<ItemPose>();
    }
    private SaveData data = new SaveData();
    private PickupItem[] items;
    private PickupItem[] allItems;
    private IntroLessonCatalog introCatalog;
    private KitchenInteractable[] introTargets;
    private PickupItem introHeldAtActivation;
    private uint introCarryAtActivation;
    private bool reviewHintRequested;
    public bool IsIntroCourse => introCatalog != null;
    public bool ControlsPracticeActive { get; private set; }
    public IntroLessonCatalog.ControlsPracticeStep[] ControlsPracticeSteps => introCatalog?.controlsPractice;
    public void BeginControlsPractice()
    {
        Save();
        ControlsPracticeActive = true;
    }
    public void EndControlsPractice() => ControlsPracticeActive = false;
    private bool grammarIdentified, grammarPicked;
    private uint grammarPickupSequence, grammarPlacementAtActivation;
    private LessonPlacementScenario placementScenario;
    private LessonScenarioSession scenarioSession;
    public bool IsScenarioTask => GrammarTask != null && GrammarTask.IsScenario;
    public LessonScenarioSession ScenarioSession => scenarioSession;
    public bool ScenarioNeedsConfirmation => IsScenarioTask && scenarioSession != null && scenarioSession.NeedsConfirmation;
    public void ConfirmScenario() { if (!ControlsPracticeActive && ScenarioNeedsConfirmation) scenarioSession.Confirm(CurrentTaskId); }
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
            if (ControlsPracticeActive) return false;
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
    public string CourseName => "A1.1–C1.2 · " + TotalStages + " Aufgaben";
    public bool IsReviewTask => IsIntroCourse && !IsPreview && !IsComplete && introCatalog.tasks[data.stage].IsReview;
    public bool ReviewHintRequested => reviewHintRequested;
    public string CurrentTaskId => IsIntroCourse && !IsComplete ? introCatalog.tasks[data.stage].id : "";
    private FixedLearningTarget[] fixedTargets;
    private Collider table;
    private PlayerInteraction player;
    private PlayerControls controls;
    private Func<string,bool> speechRelevant;
    public event Action TaskChanged;
    public int TotalStages => items == null ? 0 : introCatalog.tasks.Length;
    public bool PersistenceEnabled { get; set; } = true;
    public bool IsPreview => !data.started;
    public int Stage => data.stage;
    public bool IsComplete => items != null && data.stage >= TotalStages;
    private void Start()
    {
        player = GetComponent<PlayerInteraction>();
        controls=GetComponent<PlayerControls>();speechRelevant=IsSpeechRelevant;
        items = FindObjectsByType<PickupItem>();
        Array.Sort(items, (a, b) => { int order = a.LessonOrder.CompareTo(b.LessonOrder); return order != 0 ? order : string.CompareOrdinal(Id(a), Id(b)); });
        // Start with the spoon, then introduce the other available words.
        int spoon = Array.FindIndex(items, item => item.DisplayName == "der Löffel");
        if (spoon > 0) { PickupItem first = items[0]; items[0] = items[spoon]; items[spoon] = first; }
        fixedTargets = FindObjectsByType<FixedLearningTarget>();
        Array.Sort(fixedTargets, (a, b) => a.Order.CompareTo(b.Order));
        allItems = items;
        introCatalog = IntroLessonCatalog.Load();
        introTargets = introCatalog.Resolve(allItems, fixedTargets);
        GameObject tabletop = GameObject.Find("TableTop");
        if (tabletop != null) table = tabletop.GetComponent<Collider>();
        try
        {
            string saved = PlayerPrefs.GetString(IntroSaveKey, "");
            if (!string.IsNullOrEmpty(saved)) data = JsonUtility.FromJson<SaveData>(saved) ?? new SaveData();
        }
        catch (Exception) { data = new SaveData(); }
        if (data.completedTasks == null) data.completedTasks = new List<string>();
        data.stage = introCatalog.ResumeStage(data.completedTasks);
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

    private static string Id(PickupItem item) => item.gameObject.scene.path + "/" + item.name;

    public static void ClearSavedProgress() { PlayerPrefs.DeleteKey(IntroSaveKey); PlayerPrefs.Save(); }

    public string TaskSpeech => items == null || IsPreview ? GermanLessonSpeech.IntroWelcome :
        IsComplete ? GermanLessonSpeech.IntroComplete(TotalStages) : introCatalog.tasks[data.stage].instruction;
    public string TaskEnglish => items == null || IsPreview ? "Welcome! Start the course and discover the objects." :
        IsComplete ? "Well done! You have completed all " + TotalStages + " tasks. You can keep practising freely." :
        introCatalog.tasks[data.stage].englishInstruction;

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
        return false;
    }

    private List<string> Guidance()
    {
        if (IsScenarioTask && scenarioSession != null)
            return scenarioSession.CaseReaderOpen ? new List<string> { TaskSpeech, scenarioSession.CurrentCasePage } : GrammarAnswerOpen ? new List<string> { GrammarAnswerHelp, GrammarTask.question } : new List<string> { TaskSpeech, scenarioSession.Status };
        List<string> lines = new List<string> { TaskSpeech };

        if (IsIntroCourse && data.stage == 0 && player.HeldItem == null)
            lines.Insert(0, GermanLessonSpeech.PickupGuidance(controls!=null && controls.UsesTouch));
        if (IsIntroCourse && !IsComplete)
        {
            bool pickup = introCatalog.tasks[data.stage].RequiresPickup;
            if (GrammarAnswerOpen) return new List<string> { GrammarAnswerHelp, GrammarTask.question };
            if (IsPlacementTask && player.HeldItem == introTargets[data.stage])
                return new List<string> { TaskSpeech, "Lege den Gegenstand vollständig in die markierte Zone auf dem Tisch." };
            if (player.HeldItem != null && !player.IsPlacing)
                lines.Insert(0, pickup ? GermanLessonSpeech.ReleaseBeforeNext : GermanLessonSpeech.ReleaseOptional);
            if (!pickup && (data.stage == 0 || introCatalog.tasks[data.stage - 1].RequiresPickup))
                lines.Insert(0, GermanLessonSpeech.IdentifyGuidance(controls!=null && controls.UsesTouch));
        }
        return lines;

    }

    private void SpeakWithGuidance(params string[] opening)
    {
        List<string> lines = new List<string>(opening);
        foreach (string line in Guidance()) if (!lines.Contains(line)) lines.Add(line);
        player.Voice.SpeakContext(speechRelevant,lines.ToArray());
    }
    private bool IsSpeechRelevant(string line) => GermanLessonSpeech.StillRelevant(line,player.HeldItem!=null,player.IsPlacing,controls!=null && controls.UsesTouch);

    public void RepeatTask()
    {
        if (ControlsPracticeActive) return;
        if (player == null) return;
        SpeakWithGuidance();
    }

    public void StartLessons()
    {
        if (ControlsPracticeActive) return;
        if (items == null || data.started) return;
        data.started = true;
        CaptureIntroActivation();
        Save();
        RepeatTask();
        TaskChanged?.Invoke();
    }

    public void Found(PickupItem item)
    {
        if (ControlsPracticeActive) return;
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
            else SpeakWithGuidance((introCatalog.tasks[data.stage].RequiresPickup || IsPlacementTask) ? GermanLessonSpeech.WrongPickup :
                GermanLessonSpeech.IdentifyGuidance(controls!=null && controls.UsesTouch));
            return;
        }
    }

    public bool Identified(KitchenInteractable item)
    {
        if (ControlsPracticeActive) return false;
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
        return false;
    }

    private void CompleteIntroTask()
    {
        var task = introCatalog.tasks[data.stage];
        if (!data.completedTasks.Contains(task.id)) data.completedTasks.Add(task.id);
        data.stage = introCatalog.ResumeStage(data.completedTasks);
        CaptureIntroActivation();
        TaskChanged?.Invoke();
        Save();
        // Progress is already visible in the task card; speak only what is needed next.
        SpeakWithGuidance();
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
        if (ControlsPracticeActive) return;
        if (!IsPlacementTask && !IsScenarioTask) return;
        bool scenario = IsScenarioTask;
        CaptureIntroActivation(); Save();
        player.Voice.Speak(scenario ? "Die Übung wurde neu vorbereitet." : "Die Tischübung wurde neu angeordnet.", TaskSpeech);
    }

    public void RevealReviewTarget()
    {
        if (ControlsPracticeActive) return;
        if (!IsReviewTask || reviewHintRequested) return;
        reviewHintRequested = true;
        player.Voice.Speak(GermanLessonSpeech.ReviewHint, TaskSpeech);
    }

    public void Placed(PickupItem item)
    {
        if (ControlsPracticeActive) return;
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
        if (ControlsPracticeActive) return;
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
        PlayerPrefs.SetString(IntroSaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

}
