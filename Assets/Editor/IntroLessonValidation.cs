using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Called by the existing Play Mode harness; never writes either learning profile.
public static class IntroLessonValidation
{
    private static FieldInfo Field(string name) => typeof(LearningProgress).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }

    public static void Run(PlayerInteraction player)
    {
        LearningProgress learning = player.Learning;
        string[] fields = { "data", "items", "fixedTargets", "introCatalog", "introTargets", "introHeldAtActivation", "introCarryAtActivation", "reviewHintRequested", "grammarIdentified", "grammarPicked", "grammarPickupSequence", "grammarPlacementAtActivation" };
        object[] previous = fields.Select(name => Field(name).GetValue(learning)).ToArray();
        bool persistence = learning.PersistenceEnabled;
        LessonPresentation presentation = player.GetComponent<LessonPresentation>();
        bool presentationPersistence = presentation.PersistenceEnabled;
        var mode = presentation.Mode;
        float volume = presentation.VoiceVolume;
        bool reported = presentation.ReportedNoSound;
        bool muted = presentation.DeviceMuted;
        string oldSave = PlayerPrefs.GetString("DeutschRoom.Learning.v1", "");
        string introSave = PlayerPrefs.GetString("DeutschRoom.Learning.IntroA11.v1", "");
        bool scenarioWasActive = learning.ScenarioSession != null && learning.ScenarioSession.Active;
        if (scenarioWasActive) learning.ScenarioSession.Finish();
        bool placementWasActive = learning.PlacementScenario != null && learning.PlacementScenario.Active;
        if (placementWasActive) learning.PlacementScenario.Finish(player);
        PickupItem[] all = (PickupItem[])Field("allItems").GetValue(learning);
        var positions = all.Select(item => item.transform.position).ToArray();
        var rotations = all.Select(item => item.transform.rotation).ToArray();
        var parents = all.Select(item => item.transform.parent).ToArray();
        RoomExperience room = player.GetComponent<RoomExperience>();
        bool menuWasOpen = room.MenuOpen;
        try
        {
            room.SetMenu(false);
            learning.PersistenceEnabled = false;
            presentation.PersistenceEnabled = false;
            IntroLessonCatalog catalog = IntroLessonCatalog.Load();
            FixedLearningTarget[] fixtures = UnityEngine.Object.FindObjectsByType<FixedLearningTarget>();
            KitchenInteractable[] targets = catalog.Resolve(all, fixtures);
            Field("introCatalog").SetValue(learning, catalog);
            Field("items").SetValue(learning, all);
            Field("introTargets").SetValue(learning, targets);
            Field("fixedTargets").SetValue(learning, fixtures);
            Field("data").SetValue(learning, JsonUtility.FromJson("{\"started\":false,\"stage\":0,\"completedTasks\":[],\"poses\":[]}", Field("data").FieldType));
            Check(learning.IsIntroCourse && learning.TotalStages == IntroLessonCatalog.ImplementedTaskCount && learning.SearchCount == IntroLessonCatalog.ImplementedTaskCount && learning.IsPreview,
                "Intro course must expose exactly 430 implemented introduction/review tasks.");
            Check(Resources.Load<Material>("Lessons/PlacementZone") != null, "Placement marker material missing.");
            Check(targets[0].name == "Loeffel_Spoon" && catalog.tasks[0].instruction == "Nimm den Löffel.", "Intro lost spoon-first explicit pickup instruction.");
            Check(!learning.TryGetMapTarget(out _, out _), "Preview has a stale intro map target.");
            learning.StartLessons();
            presentation.SetVoiceVolume(1);
            presentation.ReportNoSound(false);
            presentation.SendMessage("OnDeviceMute", false);
            presentation.SetMode(LessonPresentation.OutputMode.Voice);
            learning.RepeatTask();
            Check(!player.Voice.IntroAudioPending && !player.Voice.NeedsTextFallback &&
                  presentation.PlayVoice && !presentation.ShowText,
                "Installed intro recordings did not enable voice-only narration.");
            for (int index = 0; index < catalog.tasks.Length; index++)
                foreach (string phrase in new[] { catalog.tasks[index].instruction, catalog.tasks[index].success,
                    targets[index]?.DisplayName, catalog.tasks[index].question, GermanLessonSpeech.Stage(index + 1) }.Where(phrase => !string.IsNullOrEmpty(phrase)))
                    Check(player.Voice.ClipFor(phrase) != null && player.Voice.ClipFor(phrase).length > .1f,
                        "Missing or empty intro recording: " + phrase);
            foreach (string phrase in new[] { GermanLessonSpeech.IntroComplete(IntroLessonCatalog.ImplementedTaskCount), GermanLessonSpeech.IntroWelcome,
                GermanLessonSpeech.ReleaseBeforeNext, GermanLessonSpeech.WrongPickup, GermanLessonSpeech.PickupHelp,
                GermanLessonSpeech.IdentifyHelp, GermanLessonSpeech.ReleaseOptional, GermanLessonSpeech.ReviewHint })
                Check(player.Voice.ClipFor(phrase) != null, "Missing shared intro guidance: " + phrase);
            Check(player.Voice.GetComponent<AudioSource>().clip == player.Voice.ClipFor("Aufgabe 1."),
                "Narrator did not select the actual intro pack recording.");
            player.Voice.Speak("Validation: deliberately missing intro recording.");
            Check(player.Voice.NeedsTextFallback && presentation.ShowText,
                "A missing recording left voice-only users without readable fallback.");
            learning.RepeatTask();
            Check(!player.Voice.NeedsTextFallback && !presentation.ShowText,
                "Returning to installed recordings did not restore voice-only mode.");
            presentation.SetMode(LessonPresentation.OutputMode.Text);
            for (int i = 0; i < targets.Length; i++)
            {
                if (catalog.tasks[i].IsScenario)
                {
                    ScenarioLessonValidation.RunTask(player, room, catalog.tasks[i], i);
                    Release(player); continue;
                }
                var target = targets[i];
                Check(learning.CurrentTaskId == catalog.tasks[i].id && learning.TaskSpeech == catalog.tasks[i].instruction,
                    "Intro prompt or stable task ID differs from catalog.");
                if (catalog.tasks[i].IsReview)
                {
                    Check(!learning.ReviewHintRequested && !learning.TryGetMapTarget(out _, out _), "Review revealed its target before help was requested.");
                    float scale = (float)typeof(RoomExperience).GetProperty("UIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
                    Rect safe = RoomExperience.SafeScreenArea;
                    Vector2 hint = room.ReviewHintRect.center;
                    Check(room.TryPointer(new Vector2(safe.xMin + hint.x * scale, safe.yMax - hint.y * scale)) &&
                        learning.ReviewHintRequested, "Review hint touch did not reveal the target.");
                    Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.ReviewHint), "Hint has no spoken confirmation.");
                }
                Check(learning.TryGetMapTarget(out Vector3 at, out _) && at == target.transform.position,
                    "Intro map points at the wrong actual object.");
                if (catalog.tasks[i].IsGrammar && !catalog.tasks[i].RequiresPickup)
                {
                    var task = catalog.tasks[i];
                    learning.SubmitGrammarAnswer(task.id, task.answerIndex);
                    Check(learning.Stage == i && !learning.GrammarAnswerOpen, "Answer before physical action advanced.");
                    if (task.RequiresPlacement)
                    {
                        var pickup = (PickupItem)target;
                        var scenario = learning.PlacementScenario;
                        Check(scenario != null && scenario.Active && !learning.Identified(target), "Placement preset or action gate missing.");
                        Check(!player.TryPickUp(scenario.Reference), "Reference object can leave its anchor.");
                        learning.Placed(pickup);
                        Check(!learning.GrammarAnswerOpen, "Unpicked preset object opened answers.");
                        Check(player.TryPickUp(pickup), "Cannot take placement target.");
                        player.CancelCarry(); learning.Placed(pickup);
                        Check(!learning.GrammarAnswerOpen, "Cancelled carry counted as placement.");
                        Vector3 wrongZone = pickup.transform.position;
                        Check(player.TryPickUp(pickup), "Cannot retry placement target.");
                        var table = learning.MapTable;
                        Check(table.Raycast(new Ray(wrongZone + Vector3.up * 2, Vector3.down), out RaycastHit wrong, 4) &&
                            player.TryPlaceHeld(wrong), "Wrong-zone placement could not be tested.");
                        Check(learning.Stage == i && !learning.GrammarAnswerOpen, "Wrong table zone opened answers.");
                        if (i == 250)
                        {
                            Check(scenario.TryGetOriginal(pickup, out var original), "Preset forgot original pose.");
                            var poses = (System.Collections.IEnumerable)Field("data").GetValue(learning).GetType().GetField("poses").GetValue(Field("data").GetValue(learning));
                            bool found = false;
                            foreach (object pose in poses)
                                if (((string)pose.GetType().GetField("id").GetValue(pose)).EndsWith("/" + pickup.name, StringComparison.Ordinal))
                                { found = true; Check((Vector3)pose.GetType().GetField("worldPosition").GetValue(pose) == original.position, "Temporary layout leaked into save."); }
                            Check(found, "Original placement target pose missing from save.");
                            learning.ResetPlacementPractice();
                            Check(learning.Stage == i && !learning.GrammarAnswerOpen, "Reset skipped a task or retained readiness.");
                        }
                        Check(player.TryPickUp(pickup), "Cannot take target after reset/wrong placement.");
                        Vector3 placementAt = scenario.ZonePosition;
                        Check(table.Raycast(new Ray(placementAt + Vector3.up * 2, Vector3.down), out RaycastHit correct, 4) &&
                            player.TryPlaceHeld(correct), "Cannot place in target zone.");
                    }
                    else
                    {
                        Check(!learning.Identified(all.First(item => item != target)), "Wrong portable identification accepted.");
                        Check(player.TryPickUp((PickupItem)target), "Cannot test pickup during plural task.");
                        Check(!learning.GrammarAnswerOpen && learning.Stage == i, "Pickup substituted for identification.");
                        player.CancelCarry();
                        var part = target.GetComponentInChildren<VocabularyPart>();
                        if (part != null) { player.IdentifyPart(part); Check(!learning.GrammarAnswerOpen, "Detail substituted for primary identification."); }
                        player.Identify(target);
                    }
                    Check(learning.Stage == i && learning.GrammarAnswerOpen, "Physical grammar action advanced without answer or failed to open answers.");
                    AnswerThroughPointer(player, room, task, i);
                }
                else if (catalog.tasks[i].RequiresPickup)
                {
                    var pickup = (PickupItem)target;
                    learning.Found(pickup);
                    Check(learning.Stage == i, "A direct Found call without holding skipped a task.");
                    player.Identify(target);
                    Check(learning.Stage == i, "Hearing a pickup name incorrectly completed its task.");
                    Check(player.TryPickUp(all.First(item => item != pickup)), "Cannot test wrong pickup.");
                    Check(learning.Stage == i && player.Voice.LastRequest.Contains(GermanLessonSpeech.WrongPickup),
                        "Wrong pickup advanced or lacked correction.");
                    Release(player);
                    if (catalog.tasks[i].IsGrammar)
                    {
                        learning.SubmitGrammarAnswer(catalog.tasks[i].id, catalog.tasks[i].answerIndex);
                        Check(learning.Stage == i, "Answer before pickup advanced.");
                    }
                    Check(player.TryPickUp(pickup), "Cannot take the requested target.");
                    if (catalog.tasks[i].IsGrammar)
                    {
                        var task = catalog.tasks[i];
                        Check(learning.Stage == i && learning.GrammarAnswerOpen, "Pickup alone completed grammar.");
                        learning.SubmitGrammarAnswer(task.id, (task.answerIndex + 1) % 3);
                        Check(learning.Stage == i && learning.GrammarAnswerOpen, "Wrong article advanced.");
                        float scale = (float)typeof(RoomExperience).GetProperty("UIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
                        Rect safe = RoomExperience.SafeScreenArea;
                        Vector2 choice = room.GrammarChoiceRect(task.answerIndex).center;
                        room.TryPointer(new Vector2(safe.xMin + choice.x * scale, safe.yMax - choice.y * scale));
                        learning.SubmitGrammarAnswer(task.id, task.answerIndex);
                        Check(learning.Stage == i + 1, "Correct answer touch or stale answer guard failed.");
                    }
                }
                else
                {
                    Check(!learning.Identified(fixtures.First(fixture => fixture.Item != target &&
                        !fixture.Item.transform.IsChildOf(target.transform)).Item), "Wrong fixed target advanced.");
                    Check(learning.Stage == i, "Wrong identification changed stage.");
                    var part = target.GetComponentInChildren<VocabularyPart>();
                    if (part != null)
                    {
                        player.IdentifyPart(part);
                        Check(learning.Stage == i, "Detail alone completed a whole-object task.");
                    }
                    Check(player.TryPickUp(all[0]), "Cannot test pickup during a fixed-target task.");
                    Check(learning.Stage == i, "Pickup completed a fixed identification task.");
                    if (i != 132) Release(player);
                    player.Identify(target);
                }
                Check(learning.Stage == i + 1 && player.Voice.LastRequest[0] == catalog.tasks[i].success,
                    "Actual interaction did not complete exactly one matching task.");
                if (i + 1 < targets.Length)
                {
                    Check(player.Voice.LastRequest.Contains(catalog.tasks[i + 1].instruction), "Next instruction missing.");
                    if (catalog.tasks[i].RequiresPickup && catalog.tasks[i + 1].RequiresPickup)
                        Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.ReleaseBeforeNext), "Free-hand guidance missing.");
                    if (i == 52)
                        Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IdentifyHelp), "Pickup-to-fixture transition lacks an explanation.");
                }
                else
                    Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IntroComplete(IntroLessonCatalog.ImplementedTaskCount)) &&
                          !player.Voice.LastRequest.Contains("Aufgabe 431."), "Ending announces an unimplemented task.");
                if (i == 132)
                {
                    Check(player.HeldItem != null && player.HeldItem.name == "Loeffel_Spoon", "Held-at-review-start test did not retain the spoon.");
                    learning.Found(player.HeldItem);
                    Check(learning.Stage == 133, "Already-held spoon completed its review without a fresh pickup.");
                }
                if (target is PickupItem taken) learning.Found(taken);
                else player.Identify(target);
                Check(learning.Stage == i + 1, "Repeated notification skipped a task.");
                Release(player);
            }
            Check(learning.ScenarioSession == null || !learning.ScenarioSession.Active, "Scenario preset leaked into free play.");
            Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IntroComplete(430)) && !player.Voice.LastRequest.Contains("Aufgabe 431."), "Scenario ending announces an unimplemented stage.");
            Check(learning.IsComplete && !learning.IsArticleTask && !learning.TryGetMapTarget(out _, out _),
                "Intro completion leaked into legacy article tasks or left a map marker.");
            Check(catalog.ResumeStage(new List<string> { "CUR-001", "CUR-003", "unknown" }) == 1,
                "Stable-ID resume skipped an unfinished task.");
            Check(catalog.ResumeStage(catalog.tasks.Select(task => task.id).ToList()) == 430,
                "Completed intro IDs did not restore the completed course.");
            string serialized = JsonUtility.ToJson(Field("data").GetValue(learning));
            Check(serialized.Contains("CUR-430") && serialized.Contains("completedTasks"), "Intro save omits stable completion IDs.");
            Check(PlayerPrefs.GetString("DeutschRoom.Learning.v1", "") == oldSave &&
                  PlayerPrefs.GetString("DeutschRoom.Learning.IntroA11.v1", "") == introSave,
                "Validation changed the user's learning profile.");
            Check(catalog.ResumeStage(catalog.tasks.Take(30).Select(task => task.id).ToList()) == 30,
                "A completed 30-task save failed to resume at task 31.");
            Check(catalog.ResumeStage(catalog.tasks.Take(53).Select(task => task.id).ToList()) == 53,
                "Pickup completion did not resume at the first fixed target.");
            Check(catalog.ResumeStage(catalog.tasks.Take(80).Select(task => task.id).ToList()) == 80,
                "A completed 80-task save failed to resume at task 81.");
            Check(catalog.ResumeStage(catalog.tasks.Take(130).Select(task => task.id).ToList()) == 130,
                "A completed 130-task save failed to resume at task 131.");
            Check(catalog.ResumeStage(catalog.tasks.Take(133).Select(task => task.id).ToList()) == 133,
                "Completed introductions incorrectly completed reviews of the same objects.");
            Check(learning.PlacementScenario == null || !learning.PlacementScenario.Active, "Practice layout remained after leaving placement section.");
            Check(catalog.ResumeStage(catalog.tasks.Take(230).Select(task => task.id).ToList()) == 230, "Completed 230 save failed to resume 231.");
            Debug.Log("INTRO_LESSON_VALIDATION_PASS: 430 tasks, 90 grammar and 140 scenarios, identification/placement gates, original-pose persistence, reset, answer touch, save continuation and ending without 431.");
        }
        finally
        {
            Release(player);
            if (learning.ScenarioSession != null) learning.ScenarioSession.Finish();
            if (learning.PlacementScenario != null) learning.PlacementScenario.Finish(player);
            for (int i = 0; i < all.Length; i++)
            {
                all[i].transform.SetParent(parents[i], true);
                all[i].transform.SetPositionAndRotation(positions[i], rotations[i]);
            }
            for (int i = 0; i < fields.Length; i++) Field(fields[i]).SetValue(learning, previous[i]);
            if (placementWasActive || scenarioWasActive)
                typeof(LearningProgress).GetMethod("CaptureIntroActivation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(learning, null);
            player.Voice.Stop();
            presentation.SetMode(mode);
            presentation.SetVoiceVolume(volume);
            presentation.ReportNoSound(reported);
            presentation.SendMessage("OnDeviceMute", muted);
            presentation.PersistenceEnabled = presentationPersistence;
            learning.PersistenceEnabled = persistence;
            room.SetMenu(menuWasOpen);
            Physics.SyncTransforms();
        }
    }

    private static void AnswerThroughPointer(PlayerInteraction player, RoomExperience room, IntroLessonCatalog.Task task, int stage)
    {
        var learning = player.Learning;
        learning.SubmitGrammarAnswer(task.id, (task.answerIndex + 1) % 3);
        Check(learning.Stage == stage && learning.GrammarAnswerOpen, "Wrong grammar answer advanced.");
        float scale = (float)typeof(RoomExperience).GetProperty("UIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
        Rect safe = RoomExperience.SafeScreenArea;
        Vector2 choice = room.GrammarChoiceRect(task.answerIndex).center;
        room.TryPointer(new Vector2(safe.xMin + choice.x * scale, safe.yMax - choice.y * scale));
        learning.SubmitGrammarAnswer(task.id, task.answerIndex);
        Check(learning.Stage == stage + 1, "Answer pointer or stale-answer guard failed.");
    }

    public static void SelectLegacyForValidation(PlayerInteraction player)
    {
        var learning = player.Learning;
        var all = (PickupItem[])Field("allItems").GetValue(learning);
        var sorted = all.OrderBy(item => item.LessonOrder).ThenBy(item => item.name, StringComparer.Ordinal).ToArray();
        int spoon = Array.FindIndex(sorted, item => item.DisplayName == "der Löffel");
        if (spoon > 0) { var first = sorted[0]; sorted[0] = sorted[spoon]; sorted[spoon] = first; }
        Field("introCatalog").SetValue(learning, null);
        Field("items").SetValue(learning, sorted);
        Field("fixedTargets").SetValue(learning, UnityEngine.Object.FindObjectsByType<FixedLearningTarget>().OrderBy(target => target.Order).ToArray());
        Field("data").SetValue(learning, JsonUtility.FromJson("{\"started\":false,\"stage\":0,\"pickupCount\":53,\"fixedCount\":80,\"poses\":[]}", Field("data").FieldType));
        Check(learning.TotalStages == 136, "Legacy path no longer preserves all 136 tasks.");
    }

    private static void Release(PlayerInteraction player)
    {
        if (player.HeldItem == null) return;
        player.enabled = false;
        player.enabled = true;
    }
}
