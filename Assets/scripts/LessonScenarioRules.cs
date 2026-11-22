using System;
using System.Collections.Generic;

// Schema and event freshness rules are executable without a Unity player.
public static class LessonScenarioRules
{
    private static readonly HashSet<string> Devices = new HashSet<string> { "living","kitchenTap","cupboard","drawer","fridge","washer","detergent","hood","basin","shower","lamp","flush","lid","desk","shoe" };
    public static bool BlockOpening(bool required, bool alreadyOpen, float distance) => required && !alreadyOpen && distance < .85f;
    public static bool ClearanceReached(bool rejectedAttempt, float distance) => rejectedAttempt && distance >= .95f;
    public static bool FreshState(uint current, uint baseline, bool state, bool wanted, bool settled) => current != baseline && state == wanted && settled;
    public static bool FreshPlacement(uint carry, uint initialCarry, uint placement, uint initialPlacement, uint lastPlacedCarry, bool held) =>
        !held && carry != initialCarry && placement != initialPlacement && carry == lastPlacedCarry;
    public static void Validate(IntroLessonCatalog.Task task)
    {
        if (task.steps == null || task.steps.Length == 0 || task.presets == null || task.scenarioItems == null || task.zones == null)
            throw new InvalidOperationException("Incomplete scenario.");
        var items = new HashSet<string>(); var zones = new HashSet<string>();
        foreach (var item in task.scenarioItems)
            if (item == null || string.IsNullOrEmpty(item.name) || !items.Add(item.name) ||
                (item.slot < 0 || item.slot > 3) || (item.source != "table" && item.source != "drawer" && item.source != "fridge" && item.source != "cupboard"))
                throw new InvalidOperationException("Invalid scenario item.");
        foreach (var zone in task.zones)
            if (zone == null || string.IsNullOrEmpty(zone.id) || !zones.Add(zone.id) || string.IsNullOrEmpty(zone.label) || float.IsNaN(zone.width) || float.IsNaN(zone.depth) || zone.width <= 0 || zone.depth <= 0 ||
                Math.Abs(zone.x)+zone.width/2 > 1 || Math.Abs(zone.z)+zone.depth/2 > .6)
                throw new InvalidOperationException("Invalid table zone.");
        foreach (var p in task.presets) if (p == null || !Devices.Contains(p.key)) throw new InvalidOperationException("Unknown device preset.");
        foreach (string name in task.fixedItems ?? Array.Empty<string>()) if (!items.Contains(name)) throw new InvalidOperationException("Unknown fixed reference.");
        foreach (string name in task.keepOnTable ?? Array.Empty<string>()) if (!items.Contains(name)) throw new InvalidOperationException("Unknown retained table target.");
        bool physical = false;
        foreach (var step in task.steps)
        {
            if (step == null || string.IsNullOrEmpty(step.text)) throw new InvalidOperationException("Missing scenario step prompt.");
            if (step.op != "confirm" && step.op != "cue" && step.op != "read" && step.op != "dialogue") physical = true;
            switch (step.op)
            {
                case "blocked": case "clearance": case "state": case "flush": if (!Devices.Contains(step.key) || (step.op == "flush" && step.key != "flush")) throw new InvalidOperationException("Unknown device step."); break;
                case "pickup": case "offTable": if (!items.Contains(step.key)) throw new InvalidOperationException("Unknown pickup step."); break;
                case "place": if (!items.Contains(step.key) || (step.zone != "table" && !zones.Contains(step.zone))) throw new InvalidOperationException("Unknown placement zone."); break;
                case "arrange":
                    if (step.keys == null || step.zones == null || step.keys.Length != step.zones.Length || step.keys.Length == 0) throw new InvalidOperationException("Invalid arrangement.");
                    for (int i=0;i<step.keys.Length;i++) if (!items.Contains(step.keys[i]) || !zones.Contains(step.zones[i])) throw new InvalidOperationException("Invalid arrangement target.");
                    break;
                case "allStates":
                    if (step.keys == null || step.states == null || step.keys.Length != step.states.Length || step.keys.Length == 0) throw new InvalidOperationException("Invalid all-states step.");
                    foreach (var key in step.keys) if (!Devices.Contains(key)) throw new InvalidOperationException("Unknown all-states device.");
                    break;
                case "cue": if (float.IsNaN(step.seconds) || float.IsInfinity(step.seconds) || step.seconds < 0 || (!string.IsNullOrEmpty(step.key) && !Devices.Contains(step.key))) throw new InvalidOperationException("Negative cue duration."); break;
                case "read": if (task.caseId != "E" && task.caseId != "M" && task.caseId != "W" && task.caseId != "H") throw new InvalidOperationException("Unknown case source."); break;
                case "dialogue":
                    if (string.IsNullOrWhiteSpace(step.question) || string.IsNullOrWhiteSpace(step.reply) || step.choices == null || step.choices.Length != 3 ||
                        new HashSet<string>(step.choices).Count != 3 || Array.Exists(step.choices,string.IsNullOrWhiteSpace) || step.answerIndex < 0 || step.answerIndex >= 3)
                        throw new InvalidOperationException("Invalid clarification dialogue.");
                    break;
                case "confirm": if (!string.IsNullOrEmpty(step.key) && !Devices.Contains(step.key)) throw new InvalidOperationException("Unknown confirmation device."); break;
                case "hall": break;
                default: throw new InvalidOperationException("Unknown scenario operation.");
            }
        }
        if (!string.IsNullOrEmpty(task.caseId) && ((task.caseId != "E" && task.caseId != "M" && task.caseId != "W" && task.caseId != "H") || task.steps[0].op != "read")) throw new InvalidOperationException("Case needs its source-reading step first.");
        foreach (var step in task.steps)
            if ((step.op == "blocked" || step.op == "clearance") && (step.key != task.blockedDevice || (step.key != "washer" && step.key != "cupboard")))
                throw new InvalidOperationException("Invalid clearance owner.");
        if (!string.IsNullOrEmpty(task.startHeld) && !items.Contains(task.startHeld)) throw new InvalidOperationException("Unknown held preset.");
        if (!string.IsNullOrEmpty(task.blockedDevice) && task.blockedDevice != "washer" && task.blockedDevice != "cupboard") throw new InvalidOperationException("Unsupported clearance exercise.");
        foreach (string name in task.excludedItems ?? Array.Empty<string>()) if (!items.Contains(name)) throw new InvalidOperationException("Unknown excluded target.");
        foreach (string key in task.unchangedDevices ?? Array.Empty<string>()) if (!Devices.Contains(key) || !Array.Exists(task.presets,p=>p.key==key)) throw new InvalidOperationException("Unknown preserved device.");
        foreach (var item in task.scenarioItems) if (!string.IsNullOrEmpty(item.zone) && !zones.Contains(item.zone)) throw new InvalidOperationException("Unknown initial item zone.");
        bool guardedNoAction = task.unchangedDevices != null && task.unchangedDevices.Length > 0 &&
            Array.Exists(task.steps, s => s.op == "confirm" && Array.IndexOf(task.unchangedDevices,s.key) >= 0 &&
                Array.Exists(task.presets,p => p.key == s.key && p.state == s.state));
        if (!physical && !guardedNoAction) throw new InvalidOperationException("Scenario needs a physical objective or explicit preserved-state confirmation.");
        if (!string.IsNullOrEmpty(task.forbiddenDevice) && !Devices.Contains(task.forbiddenDevice)) throw new InvalidOperationException("Unknown forbidden device.");
    }
}
