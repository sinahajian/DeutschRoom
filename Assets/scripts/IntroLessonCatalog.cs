using System;
using System.Collections.Generic;
using UnityEngine;

// Only the implemented slice is shipped. The full design is not imported as playable content.
[Serializable]
public sealed class IntroLessonCatalog
{
    public const int ImplementedTaskCount = 428;
    public string id;
    public int version;
    public Task[] tasks;
    public ControlsPracticeStep[] controlsPractice;
    public ControlGuidance[] controlGuidance;
    [Serializable] public sealed class ControlGuidance { public string id, instruction, englishInstruction; }

    [Serializable] public sealed class ControlsPracticeStep
    {
        public string id, title, instruction, englishInstruction, touchHelp, keyboardHelp;
    }

    [Serializable]
    public sealed class Task
    {
        public string id;
        public string kind;
        public string level;
        public string targetName;
        public string instruction;
        public string englishInstruction;
        public string success;
        public string reviewOf;
        public string action, question, answer, answerHelp, referenceName, relation;
        public string[] choices;
        public int answerIndex;
        public ScenarioStep[] steps;
        public ScenarioPreset[] presets;
        public ScenarioItem[] scenarioItems;
        public ScenarioZone[] zones;
        public string[] keepOnTable, fixedItems;
        public string forbiddenDevice, startHeld, blockedDevice, caseId;
        public string[] unchangedDevices, excludedItems;
        public bool IsScenario => kind == "C";
        public bool IsGrammar => kind == "G" || IsScenario;
        public string InteractionKind => IsGrammar ? action : kind;
        public bool RequiresPickup => InteractionKind == "P";
        public bool RequiresPlacement => InteractionKind == "L";
        public bool AcceptsChoice(int index) => IsGrammar && choices != null && index >= 0 && index < choices.Length && index == answerIndex;
        public bool ActionSatisfied(bool freshPickup, bool identified, bool freshPlacement) =>
            InteractionKind == "P" ? freshPickup : InteractionKind == "I" ? identified : InteractionKind == "L" && freshPlacement;
        public bool IsReview => !string.IsNullOrEmpty(reviewOf);
    }

    [Serializable] public sealed class ScenarioStep
    {
        public string op, key, text, zone, buttonText, question, reply;
        public string[] choices;
        public int answerIndex;
        public bool AcceptsDialogue(int index) => op == "dialogue" && choices != null && index >= 0 && index < choices.Length && index == answerIndex;
        public bool state;
        public string[] keys, zones;
        public bool[] states;
        public float seconds;
    }
    [Serializable] public sealed class ScenarioPreset { public string key; public bool state; }
    [Serializable] public sealed class ScenarioItem { public string name, source, zone; public int slot; }
    [Serializable] public sealed class ScenarioZone { public string id, label; public float x,z,width,depth; }

    public static IntroLessonCatalog Load()
    {
        TextAsset asset = Resources.Load<TextAsset>("Lessons/IntroA11");
        if (asset == null) throw new InvalidOperationException("Missing A1 lesson catalog.");
        var catalog = JsonUtility.FromJson<IntroLessonCatalog>(asset.text);
        if (catalog == null) throw new InvalidOperationException("Invalid lesson catalog.");
        catalog.Validate();
        return catalog;
    }

    public void Validate()
    {
        if (version != 1 || tasks == null || tasks.Length != ImplementedTaskCount)
            throw new InvalidOperationException("Expected the implemented " + ImplementedTaskCount + "-task A1 catalog.");
        if (controlsPractice == null || controlsPractice.Length != 8)
            throw new InvalidOperationException("Expected eight stage-zero controls practice steps.");
        var practiceIds = new HashSet<string>(StringComparer.Ordinal);
        if(controlGuidance==null || controlGuidance.Length!=4)throw new InvalidOperationException("Expected four translated control-mode hints.");
        foreach(var hint in controlGuidance)
            if(hint==null || string.IsNullOrWhiteSpace(hint.id) || !practiceIds.Add(hint.id) ||
                string.IsNullOrWhiteSpace(hint.instruction) || string.IsNullOrWhiteSpace(hint.englishInstruction))
                throw new InvalidOperationException("Control guidance requires unique IDs and matching English instructions.");
        foreach (var step in controlsPractice)
            if (step == null || string.IsNullOrWhiteSpace(step.id) || !practiceIds.Add(step.id) ||
                string.IsNullOrWhiteSpace(step.title) || string.IsNullOrWhiteSpace(step.instruction) ||
                string.IsNullOrWhiteSpace(step.englishInstruction) || string.IsNullOrWhiteSpace(step.touchHelp) ||
                string.IsNullOrWhiteSpace(step.keyboardHelp))
                throw new InvalidOperationException("Controls practice requires unique IDs and German/English instructions with touch/keyboard help.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        var earlier = new Dictionary<string, Task>(StringComparer.Ordinal);
        int pickups = 0, identifications = 0, reviews = 0, grammar = 0, scenarios = 0;
        foreach (Task task in tasks)
        {
            if (task == null || string.IsNullOrEmpty(task.id) || !ids.Add(task.id) ||
                string.IsNullOrEmpty(task.targetName) ||
                string.IsNullOrEmpty(task.instruction) || string.IsNullOrWhiteSpace(task.englishInstruction) || string.IsNullOrEmpty(task.success) ||
                (task.level != "A1.1" && task.level != "A1.2" && task.level != "A2.1" && task.level != "A2.2" && task.level != "B1.1" && task.level != "B1.2" && task.level != "B2.1" && task.level != "B2.2" && task.level != "C1.1" && task.level != "C1.2"))
                throw new InvalidOperationException("Invalid or duplicated lesson task.");
            if (task.IsReview)
            {
                if (!earlier.TryGetValue(task.reviewOf, out Task introduction) || introduction.IsReview ||
                    introduction.targetName != task.targetName ||
                    (!task.IsGrammar && introduction.InteractionKind != task.InteractionKind) ||
                    (task.IsGrammar && introduction.kind != "P"))
                    throw new InvalidOperationException("Review must reference the earlier introduction of the same target/action.");
                reviews++;
            }
            else if (!task.IsScenario && !targets.Add(task.targetName))
                throw new InvalidOperationException("Repeated target requires an explicit review reference.");
            earlier.Add(task.id, task);
            if (task.IsGrammar)
            {
                if ((!task.IsReview && !task.IsScenario) || (task.action != "P" && task.action != "I" && task.action != "L" && !(task.IsScenario && task.action == "S")) || string.IsNullOrEmpty(task.question) ||
                    task.choices == null || task.choices.Length != 3 || new HashSet<string>(task.choices).Count != 3 || Array.Exists(task.choices, string.IsNullOrWhiteSpace) ||
                    task.answerIndex < 0 || task.answerIndex >= 3 || task.choices[task.answerIndex] != task.answer)
                    throw new InvalidOperationException("Invalid grammar task.");
                if ((task.action == "P" && !task.instruction.StartsWith("Nimm ", StringComparison.Ordinal)) ||
                    (task.action == "I" && (!task.instruction.StartsWith("Finde ", StringComparison.Ordinal) || !task.instruction.Contains("Anhören"))) ||
                    (task.action == "L" && !task.instruction.StartsWith("Lege ", StringComparison.Ordinal) && !task.instruction.StartsWith("Stelle ", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Grammar instruction does not match its physical action.");
                if (task.RequiresPlacement && (string.IsNullOrEmpty(task.referenceName) || task.referenceName == task.targetName ||
                    (task.relation != "neben" && task.relation != "vor" && task.relation != "hinter")))
                    throw new InvalidOperationException("Invalid placement relation.");
                if (task.IsScenario) { LessonScenarioRules.Validate(task); scenarios++; }
                else grammar++;
            }
            else if (task.kind == "P" && (task.instruction.StartsWith("Nimm ", StringComparison.Ordinal) ||
                (task.IsReview && task.instruction.Contains(" Nimm ")))) pickups++;
            else if (task.kind == "I" && task.instruction.StartsWith("Finde ", StringComparison.Ordinal) &&
                     task.instruction.Contains("Anhören")) identifications++;
            else throw new InvalidOperationException("A1 task instruction does not match its action.");
        }
        if (pickups != 106 || identifications != 92 || reviews != 156 || grammar != 90 || scenarios != 140 || targets.Count != 132 || tasks[0].targetName != "Loeffel_Spoon")
            throw new InvalidOperationException("Expected spoon first, 132 introductions, 66 contextual reviews, 90 grammar tasks and 140 scenarios, 106 pickup/92 identification tasks.");
    }

    public KitchenInteractable[] Resolve(PickupItem[] pickups, FixedLearningTarget[] fixtures)
    {
        var resolved = new KitchenInteractable[tasks.Length];
        for (int i = 0; i < tasks.Length; i++)
        {
            if (tasks[i].IsScenario) continue; // Scenario bindings are validated by its independent preset.
            KitchenInteractable target = null;
            if (tasks[i].RequiresPickup || tasks[i].RequiresPlacement || tasks[i].IsGrammar)
            {
                foreach (PickupItem item in pickups)
                    if (item.name == tasks[i].targetName) Bind(ref target, item);
            }
            else
            {
                foreach (FixedLearningTarget fixture in fixtures)
                    if (fixture.name == tasks[i].targetName) Bind(ref target, fixture.Item);
            }
            if (tasks[i].RequiresPlacement && Array.Find(pickups, item => item.name == tasks[i].referenceName) == null)
                throw new InvalidOperationException("Missing placement reference: " + tasks[i].referenceName);
            if (target == null) throw new InvalidOperationException("Missing A1 target: " + tasks[i].targetName);
            string name = GermanLessonSpeech.Accusative(target.DisplayName);
            if (!tasks[i].IsReview && (tasks[i].RequiresPickup ? tasks[i].instruction != "Nimm " + name + "." :
                !tasks[i].instruction.StartsWith("Finde " + name + " ", StringComparison.Ordinal)))
                throw new InvalidOperationException("A1 instruction does not match target: " + target.name);
            resolved[i] = target;
        }
        return resolved;
    }

    private static void Bind(ref KitchenInteractable target, KitchenInteractable candidate)
    {
        if (candidate == null || target != null) throw new InvalidOperationException("Missing or ambiguous A1 target binding.");
        target = candidate;
    }

    public int ResumeStage(List<string> completed)
    {
        for (int i = 0; i < tasks.Length; i++)
            if (completed == null || !completed.Contains(tasks[i].id)) return i;
        return tasks.Length;
    }
}
