using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Called by the existing Play Mode harness; never writes the learning profile.
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
        room.CloseTutorial();
        typeof(RoomExperience).GetField("tutorialAutoPending", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(room, false);
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
            Check(learning.IsIntroCourse && learning.TotalStages == IntroLessonCatalog.ImplementedTaskCount && learning.IsPreview,
                "Intro course must expose exactly 428 implemented introduction/review tasks.");
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
                GermanLessonSpeech.IdentifyHelp, GermanLessonSpeech.ReleaseOptional, GermanLessonSpeech.ReviewHint,
                GermanLessonSpeech.PickupHelpTouch,GermanLessonSpeech.PickupHelpKeyboard,
                GermanLessonSpeech.IdentifyHelpTouch,GermanLessonSpeech.IdentifyHelpKeyboard })
                Check(player.Voice.ClipFor(phrase) != null, "Missing shared intro guidance: " + phrase);
            Check(player.Voice.GetComponent<AudioSource>().clip == player.Voice.ClipFor(player.Voice.LastRequest[0]) &&
                  !player.Voice.LastRequest.Contains("Aufgabe 1."),
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
                        FinishPlacement(player);
                        Check(learning.Stage == i && !learning.GrammarAnswerOpen, "Wrong table zone opened answers.");
                        if (catalog.tasks[i].id == "CUR-251")
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
                        FinishPlacement(player);
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
                    if (i != 131) Release(player);
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
                        Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IdentifyGuidance(player.GetComponent<PlayerControls>().UsesTouch)), "Pickup-to-fixture transition lacks a device-appropriate explanation.");
                }
                else
                    Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IntroComplete(IntroLessonCatalog.ImplementedTaskCount)) &&
                          !player.Voice.LastRequest.Contains("Aufgabe 431."), "Ending announces an unimplemented task.");
                if (i == 131)
                {
                    Check(player.HeldItem != null && player.HeldItem.name == "Loeffel_Spoon", "Held-at-review-start test did not retain the spoon.");
                    learning.Found(player.HeldItem);
                    Check(learning.Stage == 132, "Already-held spoon completed its review without a fresh pickup.");
                }
                if (target is PickupItem taken) learning.Found(taken);
                else player.Identify(target);
                Check(learning.Stage == i + 1, "Repeated notification skipped a task.");
                Release(player);
            }
            Check(learning.ScenarioSession == null || !learning.ScenarioSession.Active, "Scenario preset leaked into free play.");
            Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.IntroComplete(IntroLessonCatalog.ImplementedTaskCount)) && !player.Voice.LastRequest.Contains("Aufgabe 431."), "Scenario ending announces an unimplemented stage.");
            Check(learning.IsComplete && !learning.TryGetMapTarget(out _, out _),
                "Curriculum completion left an active task or map marker.");
            Check(catalog.ResumeStage(new List<string> { "CUR-001", "CUR-003", "unknown" }) == 1,
                "Stable-ID resume skipped an unfinished task.");
            Check(catalog.ResumeStage(catalog.tasks.Select(task => task.id).ToList()) == IntroLessonCatalog.ImplementedTaskCount,
                "Completed intro IDs did not restore the completed course.");
            string serialized = JsonUtility.ToJson(Field("data").GetValue(learning));
            Check(serialized.Contains("CUR-430") && serialized.Contains("completedTasks"), "Intro save omits stable completion IDs.");
            Check(PlayerPrefs.GetString("DeutschRoom.Learning.IntroA11.v1", "") == introSave,
                "Validation changed the user's learning profile.");
            Check(catalog.ResumeStage(catalog.tasks.Take(30).Select(task => task.id).ToList()) == 30,
                "A completed 30-task save failed to resume at task 31.");
            Check(catalog.ResumeStage(catalog.tasks.Take(53).Select(task => task.id).ToList()) == 53,
                "Pickup completion did not resume at the first fixed target.");
            Check(catalog.ResumeStage(catalog.tasks.Take(80).Select(task => task.id).ToList()) == 80,
                "A completed 80-task save failed to resume at task 81.");
            Check(catalog.ResumeStage(catalog.tasks.Take(130).Select(task => task.id).ToList()) == 130,
                "A completed 130-task save failed to resume at task 131.");
            Check(catalog.ResumeStage(catalog.tasks.Take(132).Select(task => task.id).ToList()) == 132,
                "Completed introductions incorrectly completed reviews of the same objects.");
            Check(learning.PlacementScenario == null || !learning.PlacementScenario.Active, "Practice layout remained after leaving placement section.");
            Check(catalog.ResumeStage(catalog.tasks.Take(230).Select(task => task.id).ToList()) == 230, "Completed 230 save failed to resume 231.");
            ValidateTutorial(player);
            ValidateHandsOnTutorial(player);
            Debug.Log("INTRO_LESSON_VALIDATION_PASS: 428 tasks, 90 grammar and 140 scenarios, identification/placement gates, original-pose persistence, reset, answer touch, save continuation and ending without 431.");
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



    private static void ValidateTutorial(PlayerInteraction player)
    {
        var room = player.GetComponent<RoomExperience>();
        var controls = player.GetComponent<PlayerControls>();
        const string key = "DeutschRoom.ControlsTutorial.v1";
        bool hadKey = PlayerPrefs.HasKey(key), menu = room.MenuOpen;
        int seen = PlayerPrefs.GetInt(key, 0), stage = player.Learning.Stage;
        var entered = typeof(RoomExperience).GetField("entered", BindingFlags.Instance | BindingFlags.NonPublic);
        bool wasEntered = (bool)entered.GetValue(room);
        try
        {
            PlayerPrefs.DeleteKey(key);
            room.OpenControlsReference();
            Check(room.TutorialOpen && !room.MenuOpen && Time.timeScale == 0 && !controls.GameplayActive,
                "Tour did not show over the paused game or allowed gameplay input.");
            Check(PlayerPrefs.GetInt(key, 0) == 1, "First presentation was not remembered before completion.");
            float scale = (float)typeof(RoomExperience).GetProperty("UIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
            Action<Rect> click = rect => {
                Rect safe = RoomExperience.SafeScreenArea;
                Vector2 point = rect.center;
                Check(room.TryPointer(new Vector2(safe.xMin + point.x * scale, safe.yMax - point.y * scale)), "Tour pointer was not consumed.");
            };
            for (int i = 0; i < room.TutorialStepCount; i++)
            {
                Check(room.TutorialStep == i && room.TutorialOpen, "Tour skipped or repeated a step.");
                Check(!room.TutorialNextRect.Overlaps(room.TutorialHighlightRect), "Tour navigation hides the highlighted control.");
                var card = (Rect)typeof(RoomExperience).GetProperty("TutorialCard", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
                var area = (Rect)typeof(RoomExperience).GetProperty("UIArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
                Check(card.width >= 220 && card.height >= 140 && area.Contains(card.min) && area.Contains(card.max - Vector2.one * .01f),
                    "Tour card is empty or outside the safe area.");
                Check(!card.Overlaps(room.TutorialHighlightRect) && !room.TutorialBackRect.Overlaps(room.TutorialNextRect) &&
                    !room.TutorialSkipRect.Overlaps(room.TutorialNextRect), "Tour card or navigation overlaps.");
                Check(!string.IsNullOrEmpty((string)typeof(RoomExperience).GetProperty("TutorialAction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room)),
                    "Tour step is missing its short action instruction.");
                Check(room.TryPointer(new Vector2(-100,-100)) && player.Learning.Stage == stage,
                    "Outside tap leaked into lesson progress.");
                click(room.TutorialNextRect);
            }
            Check(!room.TutorialOpen && !room.MenuOpen && Time.timeScale > 0 && player.Learning.Stage == stage,
                "Finishing the tour did not resume the same task.");
            room.SetMenu(true);
            room.OpenControlsReference();
            Check(room.TutorialOpen && room.TutorialStep == 0, "Menu replay failed for a seen tour.");
            click(room.TutorialNextRect); click(room.TutorialBackRect);
            Check(room.TutorialStep == 0, "Back did not return to the previous step.");
            click(room.TutorialSkipRect);
            Check(!room.TutorialOpen && player.Learning.Stage == stage, "Skip advanced a task or left the overlay open.");
            Debug.Log("SPOTLIGHT_TUTORIAL_VALIDATION_PASS: persisted first presentation, modal input, all steps, safe-area pointer, finish, menu replay, back and skip.");
        }
        finally
        {
            room.CloseTutorial();
            entered.SetValue(room, wasEntered);
            room.SetMenu(menu);
            if (hadKey) PlayerPrefs.SetInt(key, seen); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    private static void ValidateHandsOnTutorial(PlayerInteraction player)
    {
        var room = player.GetComponent<RoomExperience>();
        var controls = player.GetComponent<PlayerControls>();
        var modeField = typeof(PlayerControls).GetField("controlMode", BindingFlags.Instance | BindingFlags.NonPublic);
        var mode = (PlayerControls.ControlMode)modeField.GetValue(controls);
        const string seenKey = "DeutschRoom.HandsOnTutorial.v1";
        bool hadSeen = PlayerPrefs.HasKey(seenKey), menu = room.MenuOpen;
        int seen = PlayerPrefs.GetInt(seenKey, 0), stage = player.Learning.Stage;
        string saved = PlayerPrefs.GetString("DeutschRoom.Learning.IntroA11.v1", "");
        PickupItem originallyHeld = player.HeldItem;
        var spoon = UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsInactive.Include).First(p => p.name == "Loeffel_Spoon");
        Vector3 position = spoon.transform.position, playerPosition = player.transform.position;
        Quaternion rotation = spoon.transform.rotation;
        uint carry = spoon.CarrySequence, place = spoon.PlacementSequence, turned = player.RotationSequence;
        try
        {
            controls.SetMode(PlayerControls.ControlMode.Touch);
            room.OpenTutorial();
            var practice = room.HandsOn;
            Check(room.HandsOnActive && !room.ModalOpen && controls.GameplayActive && Time.timeScale > 0 && player.Learning.ControlsPracticeActive,
                "Hands-on practice did not enable real gameplay and isolate lessons.");
            Action tick = () => practice.SendMessage("Update");
            room.AdvanceTutorial(); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Move, "Next/idle input bypassed required walking.");
            player.GetComponent<CharacterController>().Move(Vector3.forward * .7f); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Table, "Actual movement did not complete walking.");
            tick(); Check(practice.Step == HandsOnTutorial.PracticeStep.Table, "Walking step skipped arrival at the table.");
            // Deterministic setup for the later real action callbacks, not a claim of elapsed walking input.
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = practice.Destination; controller.enabled = true; Physics.SyncTransforms(); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Aim, "Arrival did not unlock aiming.");
            player.Identify(spoon); Check(string.IsNullOrEmpty(practice.GermanName), "Naming bypassed the aiming step.");
            typeof(PlayerInteraction).GetField("focused", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, spoon); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Name, "Aiming did not unlock naming.");
            player.Identify(spoon); tick();
            Check(practice.GermanName == "der Löffel" && practice.Step == HandsOnTutorial.PracticeStep.Pickup, "Actual naming did not display the article/name.");
            Check(player.TryPickUp(spoon), "Practice spoon could not be picked up."); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Rotate && player.Learning.Stage == stage, "Practice pickup advanced the real lesson.");
            controls.RequestAction(PlayerAction.Rotate); controls.SendMessage("Update"); player.SendMessage("Update"); tick();
            Check(practice.Step == HandsOnTutorial.PracticeStep.Place, "Real rotation input did not advance practice.");
            Collider table = GameObject.Find("TableTop").GetComponent<Collider>();
            Check(table.Raycast(new Ray(table.bounds.center + Vector3.up * 2, Vector3.down), out RaycastHit hit, 3), "Missing table placement hit.");
            Check(player.TryPlaceHeld(hit), "Practice table placement failed.");
            Check(player.IsPlacing && player.HeldItem==spoon && !practice.Complete,"Placement committed before its animation finished.");
            FinishPlacement(player); tick();
            Check(practice.Complete && player.HeldItem == null && player.Learning.Stage == stage, "Placement did not complete isolated practice.");
            room.CloseTutorial();
            Check(!room.HandsOnActive && !player.Learning.ControlsPracticeActive &&
                (spoon.transform.position-position).sqrMagnitude < .000001f && Quaternion.Angle(spoon.transform.rotation,rotation)<.01f &&
                (player.transform.position-playerPosition).sqrMagnitude < .000001f && spoon.CarrySequence==carry && spoon.PlacementSequence==place && player.RotationSequence==turned,
                "Practice changed poses or left fresh-action counters in the real lesson.");
            room.OpenTutorial(); practice = room.HandsOn;
            typeof(HandsOnTutorial).GetProperty("Step").SetValue(practice, HandsOnTutorial.PracticeStep.Pickup);
            Check(player.TryPickUp(spoon), "Mid-carry Skip setup failed.");
            typeof(HandsOnTutorial).GetProperty("Step").SetValue(practice, HandsOnTutorial.PracticeStep.Place);
            Check(player.TryPlaceHeld(hit) && player.IsPlacing,"Mid-release Skip setup failed.");
            float scale = (float)typeof(RoomExperience).GetProperty("UIScale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
            Rect safe = RoomExperience.SafeScreenArea; Vector2 skip = room.HandsOnSkipRect.center;
            Check(room.TryPointer(new Vector2(safe.xMin+skip.x*scale,safe.yMax-skip.y*scale)), "Practice Skip tap was not consumed.");
            Check(!room.TutorialOpen && !player.IsPlacing && player.HeldItem==originallyHeld && player.Learning.Stage==stage && spoon.CarrySequence==carry && spoon.PlacementSequence==place,
                "Mid-carry Skip leaked practice state/progress.");
            Check(PlayerPrefs.GetString("DeutschRoom.Learning.IntroA11.v1", "") == saved, "Practice modified the saved learning profile.");
            Debug.Log("HANDS_ON_TUTORIAL_VALIDATION_PASS: live controls, action gates, completion, saved-task isolation, pose/counter restoration and mid-carry Skip.");
        }
        finally
        {
            room.CloseTutorial(); controls.SetMode(mode); room.SetMenu(menu);
            if (hadSeen) PlayerPrefs.SetInt(seenKey, seen); else PlayerPrefs.DeleteKey(seenKey);
            PlayerPrefs.Save();
        }
    }

    // Execute the same production transaction with deterministic elapsed time, not a bypass commit.
    public static void FinishPlacement(PlayerInteraction player)
    {
        Check(player.IsPlacing,"No placement animation to settle.");
        var advance=typeof(PlayerInteraction).GetMethod("AdvancePlacement",BindingFlags.Instance|BindingFlags.NonPublic);
        PickupItem item=player.HeldItem;uint before=item.PlacementSequence;
        advance.Invoke(player,new object[]{0f});
        Check(player.HeldItem==item && item.PlacementSequence==before,"Paused placement committed early.");
        advance.Invoke(player,new object[]{.1f});
        Check(player.IsPlacing && item.PlacementSequence==before,"Partial placement committed early.");
        advance.Invoke(player,new object[]{.2f});
        Check(!player.IsPlacing && player.HeldItem==null && item.PlacementSequence==unchecked(before+1),"Placement did not commit exactly once.");
        advance.Invoke(player,new object[]{1f});
        Check(item.PlacementSequence==unchecked(before+1),"Completed placement committed twice.");
    }

    public static void CheckIdentificationVoice(PlayerInteraction player, string name)
    {
        Check(!string.IsNullOrWhiteSpace(name), "Missing identification name.");
        Check(player.Voice.ClipFor(name) != null,
            "Missing bundled identification recording: " + name);
    }

    private static void Release(PlayerInteraction player)
    {
        if (player.HeldItem == null) return;
        player.enabled = false;
        player.enabled = true;
    }
}
