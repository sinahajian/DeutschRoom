using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Also callable with -batchmode -nographics -executeMethod InteractionValidation.Run.
[InitializeOnLoad]
public static class InteractionValidation
{
    private const string ActiveKey = "DeutschRoom.InteractionValidation";
    private static int stage;
    private static double deadline;
    private static Openable[] doors;
    private static Vector3[] positions;
    private static Quaternion[] rotations;
    private static PlayerInteraction player;
    private static bool failed;
    private static PlayerControls controls;
    private static FirstPersonMovement movement;
    private static Transform cameraTransform;
    private static Openable entrance;
    private static PickupItem touchPlate;
    private static Vector3 drawerItemPosition;
    private static float originalHeight;
    private static GameObject postureCeiling;

    static InteractionValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (SessionState.GetBool(ActiveKey, false) && EditorApplication.isPlaying)
            Begin();
    }

    [MenuItem("Tools/DeutschRoom/Validate Interactions (Play Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start validation outside Play Mode.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            Begin();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(ActiveKey, false);
            if (Application.isBatchMode)
                EditorApplication.Exit(failed ? 1 : 0);
        }
    }

    private static void Begin()
    {
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 1;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < deadline || !EditorApplication.isPlaying)
            return;
        try
        {
            if (stage == 0)
            {
                player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
                player.Learning.PersistenceEnabled = false;
                IntroLessonValidation.Run(player);
                IntroLessonValidation.SelectLegacyForValidation(player);
                controls = player.GetComponent<PlayerControls>();
                Check(controls != null, "Shared desktop/touch controls missing.");
                player.GetComponent<RoomExperience>().SetMenu(false);
                CheckPlayerBody();
                TestTouchGestures();
                doors = UnityEngine.Object.FindObjectsByType<Openable>(FindObjectsSortMode.None);
                PickupItem[] items = UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
                Check(player != null && player.enabled, "Player interaction/camera binding missing.");
                Check(doors.Length == 20 + HomeExpansionBuilder.AddedDoors + BathroomFurnishingBuilder.AddedDoors + BedroomFurnishingBuilder.AddedDoors + LivingHallFurnishingBuilder.AddedDoors + HouseAccessoryBuilder.AddedDoors, "Expected kitchen doors plus four room doors.");
                Check(items.Length == 17 + KitchenContentBuilder.AddedPickups, "Unexpected portable object count.");
                VisualPolishValidation.Run();
                BedroomVocabularyValidation.Run(player);
                LivingHallVocabularyValidation.Run(player);
                HouseAccessoryValidation.Run(player);
                FinalFurnishingValidation.Run(player);
                BathroomVocabularyValidation.Run(player);
                TestNewContent();
                TestContentOcclusion(false);
                drawerItemPosition = GameObject.Find("Contents_Spatula").transform.position;
                positions = new Vector3[doors.Length];
                rotations = new Quaternion[doors.Length];
                for (int i = 0; i < doors.Length; i++)
                {
                    positions[i] = doors[i].transform.localPosition;
                    rotations[i] = doors[i].transform.localRotation;
                    doors[i].Interact(player);
                }
                foreach (PickupItem item in items)
                    Check(item.LocalBounds.size.sqrMagnitude > .00001f, "Missing pickup geometry: " + item.name);
                stage = 1;
                deadline = EditorApplication.timeSinceStartup + 1.5;
                return;
            }
            if (stage == 1)
            {
                HomePresentationValidation.CheckHome(player);
                TestContentOcclusion(true);
                TestDrawerPlacement();
                Check(Vector3.Distance(drawerItemPosition, GameObject.Find("Contents_Spatula").transform.position) > .2f,
                    "Drawer contents did not move with the open drawer.");
                for (int i = 0; i < doors.Length; i++)
                {
                    Check(doors[i].IsOpen && !doors[i].IsMoving, "Door failed to finish opening: " + doors[i].name);
                    Check(Vector3.Distance(positions[i], doors[i].transform.localPosition) > .1f ||
                          Quaternion.Angle(rotations[i], doors[i].transform.localRotation) > 70f,
                          "Hinge/slide did not move: " + doors[i].name);
                    doors[i].Interact(player);
                }
                TestPickupAndPlacement();
                TestFaucetAndPlayerCollision();
                stage = 2;
                deadline = EditorApplication.timeSinceStartup + 1.5;
                return;
            }
            if (stage == 2)
            {
                for (int i = 0; i < doors.Length; i++)
                {
                    Check(!doors[i].IsOpen && !doors[i].IsMoving, "Door failed to close: " + doors[i].name);
                    Check(Vector3.Distance(positions[i], doors[i].transform.localPosition) < .001f &&
                          Quaternion.Angle(rotations[i], doors[i].transform.localRotation) < .1f,
                          "Door did not return to its original pose: " + doors[i].name);
                }
                controls.SetMode(PlayerControls.ControlMode.Touch);
                // A headless editor has no focused window. Model returning to the app
                // before exercising the same foreground input path used on a phone.
                controls.SendMessage("OnApplicationFocus", true);
                controls.SendMessage("OnApplicationPause", false);
                Check(controls.UsesTouch && controls.GameplayActive && Cursor.lockState == CursorLockMode.None,
                    "Touch mode depends on a locked cursor.");
                MobileControlsUI ui = player.GetComponent<MobileControlsUI>();
                ui.RefreshLayout();
                Check(ui.IsVisible && ui.Layout.JoystickRadius > 0, "Touch overlay or joystick layout missing.");
                CheckContextualButtons(ui);
                TestExperience();
                TestObjectAndPartControls();
                movement = player.GetComponent<FirstPersonMovement>();
                movement.enabled = false;
                cameraTransform = player.GetComponentInChildren<Camera>().transform;
                player.transform.position = new Vector3(2.8f, 0, -3);
                cameraTransform.LookAt(GameObject.Find("Door_Leaf").GetComponent<Collider>().bounds.center);
                entrance = GameObject.Find("Door_Leaf").GetComponentInParent<Openable>();
                Physics.SyncTransforms();
                controls.RequestAction(PlayerAction.Interact);
                stage = 3;
                deadline = EditorApplication.timeSinceStartup + 1;
                return;
            }
            if (stage == 3)
            {
                Check(entrance.IsOpen && !entrance.IsMoving, "Touch interact action did not open the focused door.");
                cameraTransform.LookAt(GameObject.Find("Door_Leaf").GetComponent<Collider>().bounds.center);
                controls.RequestAction(PlayerAction.Interact);
                stage = 4;
                deadline = EditorApplication.timeSinceStartup + 1;
                return;
            }
            if (stage == 4)
            {
                Check(!entrance.IsOpen && !entrance.IsMoving, "Touch interact action did not close the focused door.");
                touchPlate = GameObject.Find("Teller_Plate").GetComponent<PickupItem>();
                player.transform.position = new Vector3(1.4f, 0, -3);
                cameraTransform.LookAt(touchPlate.transform.position + Vector3.up * .015f);
                Physics.SyncTransforms();
                controls.RequestAction(PlayerAction.Interact);
                stage = 5;
                deadline = EditorApplication.timeSinceStartup + .25;
                return;
            }
            if (stage == 5)
            {
                Check(player.HeldItem == touchPlate, "Touch action did not pick up the focused plate.");
                MobileControlsUI heldUI = player.GetComponent<MobileControlsUI>();
                heldUI.RefreshLayout();
                CheckContextualButtons(heldUI);
                Check(heldUI.Layout.RotateButton.width > 0, "Held item has no rotate control.");
                cameraTransform.LookAt(new Vector3(2, 0, -1.6f));
                controls.RequestAction(PlayerAction.Rotate | PlayerAction.Place);
                stage = 6;
                deadline = EditorApplication.timeSinceStartup + .25;
                return;
            }
            if (stage >= 7) { TestPostureStages(); return; }
            Check(player.HeldItem == null && !touchPlate.IsHeld, "Touch place action did not place the carried plate.");
            Check(Mathf.Abs(Mathf.DeltaAngle(touchPlate.transform.eulerAngles.y, 45)) < .1f,
                "Touch rotate action did not rotate the placed item.");
            controls.SetMode(PlayerControls.ControlMode.KeyboardAndMouse);
            Check(!controls.UsesTouch && !player.GetComponent<MobileControlsUI>().IsVisible,
                "Desktop mode did not hide the touch overlay.");
            movement.enabled = true;
            originalHeight = player.GetComponent<CharacterController>().height;
            Check(movement.ChangePosture(1), "Cannot enter sitting posture.");
            stage = 7;
            deadline = EditorApplication.timeSinceStartup + .4;

        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Finish(true);
        }
    }

    private static void TestDrawerPlacement()
    {
        PickupItem item = GameObject.Find("Contents_Spatula").GetComponent<PickupItem>();
        Transform originalParent = item.transform.parent;
        Vector3 originalPosition = item.transform.position;
        Quaternion originalRotation = item.transform.rotation;
        Transform drawer = GameObject.Find("Schublade_Drawer").transform;
        Collider table = GameObject.Find("TableTop").GetComponent<Collider>();
        Check(player.TryPickUp(item), "Cannot pick up an object from the open drawer.");
        Check(table.Raycast(new Ray(new Vector3(.6f, 2, 1.1f), Vector3.down), out RaycastHit top, 3) && player.TryPlaceHeld(top),
            "Cannot place drawer contents on a static surface.");
        Check(!item.transform.IsChildOf(drawer), "Placed item remains attached to drawer.");
        Vector3 placedPosition = item.transform.position;
        Vector3 drawerPosition = drawer.localPosition;
        drawer.localPosition += Vector3.back * .05f;
        Check(Vector3.Distance(item.transform.position, placedPosition) < .001f, "Table item moved with drawer.");
        drawer.localPosition = drawerPosition;
        Physics.SyncTransforms();
        Check(player.TryPickUp(item), "Cannot pick up detached drawer item.");
        Collider bottom = GameObject.Find("Drawer_Bottom").GetComponent<Collider>();
        Check(bottom.Raycast(new Ray(bottom.bounds.center + Vector3.up * .3f, Vector3.down), out RaycastHit inside, 1) && player.TryPlaceHeld(inside),
            "Cannot return item to the drawer.");
        Check(item.transform.IsChildOf(drawer), "Returned object does not follow its drawer.");
        item.transform.SetParent(originalParent, true);
        item.transform.SetPositionAndRotation(originalPosition, originalRotation);
        Physics.SyncTransforms();
    }

    private static void TestContentOcclusion(bool open)
    {
        foreach (string name in new[] { "Contents_Cup", "Contents_Milk" })
        {
            PickupItem item = GameObject.Find(name).GetComponent<PickupItem>();
            Vector3 target = item.transform.position + Vector3.up * (name == "Contents_Cup" ? .065f : .105f);
            Check(Physics.Raycast(target + Vector3.back, Vector3.forward, out RaycastHit hit, 1.2f), "Content visibility ray missed " + name);
            Check((hit.collider.GetComponentInParent<PickupItem>() == item) == open,
                "Door occlusion did not match open/closed state for " + name);
        }
    }

    private static void TestNewContent()
    {
        Check(GameObject.Find("Contents_Spatula").transform.IsChildOf(GameObject.Find("Schublade_Drawer").transform),
            "Cutlery is not attached to the moving drawer.");
        Check(GameObject.Find("Contents_Milk").GetComponent<PickupItem>().DisplayName == "die Milch", "Fridge milk missing.");
        Check(GameObject.Find("Detail_Trap").GetComponent<VocabularyObject>().DisplayName == "der Siphon", "Sink trap missing.");
        Check(GameObject.Find("Detail_WastePipe").GetComponentsInChildren<Collider>().Length > 0, "Waste pipe has no identifiable geometry.");
        foreach (string chair in new[] { "Stuhl_Chair_Front_Left", "Stuhl_Chair_Front_Right", "Stuhl_Chair_Back_Left", "Stuhl_Chair_Back_Right" })
            Check(GameObject.Find(chair).GetComponent<VocabularyObject>().DisplayName == "der Stuhl", "Unlabeled chair.");
        Check(LearningProgress.RestoreStage(5, 0, 53) == 5 && LearningProgress.RestoreStage(17, 0, 53) == 53 &&
              LearningProgress.RestoreStage(20, 17, 53) == 56 && LearningProgress.RestoreStage(53, 53, 53) == 53,
            "Existing saves lost their current vocabulary/article stage after content expansion.");
    }

    private static void TestPostureStages()
    {
        CharacterController body = player.GetComponent<CharacterController>();
        if (stage == 7)
        {
            Check(movement.Posture == FirstPersonMovement.BodyPosture.Sitting && body.height < 1.1f && cameraTransform.localPosition.y < 1.2f,
                "Sitting failed to lower the body and camera.");
            Check(movement.ChangePosture(1), "Cannot enter lying posture.");
            stage = 8; deadline = EditorApplication.timeSinceStartup + .3; return;
        }
        if (stage == 8)
        {
            Check(movement.Posture == FirstPersonMovement.BodyPosture.Lying && body.height < .8f && cameraTransform.localPosition.y < .55f,
                "Lying failed to lower the camera/collision capsule.");
            Check(movement.ChangePosture(-1), "Cannot rise to sitting in free space.");
            stage = 9; deadline = EditorApplication.timeSinceStartup + .3; return;
        }
        if (stage == 9)
        {
            postureCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postureCeiling.transform.position = player.transform.position + Vector3.up * 1.35f;
            postureCeiling.transform.localScale = new Vector3(1, .2f, 1);
            Physics.SyncTransforms();
            Check(!movement.ChangePosture(-1), "Standing passed through a low ceiling.");
            UnityEngine.Object.DestroyImmediate(postureCeiling);
            Physics.SyncTransforms();
            Check(movement.ChangePosture(-1), "Cannot stand after obstruction is removed.");
            stage = 10; deadline = EditorApplication.timeSinceStartup + .4; return;
        }
        Check(movement.Posture == FirstPersonMovement.BodyPosture.Standing && Mathf.Abs(body.height - originalHeight) < .01f,
            "Standing did not restore original collision height.");
        HomePresentationValidation.CheckPresentation(player);
        LessonGuidanceValidation.Run(player);
        Debug.Log("INTERACTION_VALIDATION_PASS: cabinet/fridge contents and moving drawer; labeled furniture/plumbing and narration; saved-stage migration; swipe-hold ownership and cancellation; sitting/lying/standing and blocked headroom; doors, pickup, placement, faucet, mobile controls, pause and music.");
        Finish(false);
    }

    private static void CheckPlayerBody()
    {
        FirstPersonBody body = player.GetComponent<FirstPersonBody>();
        Check(body != null && body.VisualRoot != null, "Player avatar was not initialized.");
        Check(body.VisualRoot.GetComponentsInChildren<Collider>().Length == 0, "Avatar visuals introduced collision or blocked interactions.");
        Check(body.VisualRoot.Find("Left hand") != null && body.VisualRoot.Find("Right hand") != null &&
            body.VisualRoot.Find("Left sneaker") != null && body.VisualRoot.Find("Right sneaker") != null, "Avatar is missing hands or feet.");
        foreach (Renderer renderer in body.VisualRoot.Find("Head").GetComponentsInChildren<Renderer>())
            Check(renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly, "Own head can obstruct the first-person camera.");
        Debug.Log("PLAYER_BODY_VALIDATION_PASS: visible body, hands/feet, head exclusion and no added physics.");
    }

    private static void CheckContextualButtons(MobileControlsUI ui)
    {
        Check((ui.Layout.InteractButton.width > 0) == player.CanUseFocused, "Use button visibility does not match usable target.");
        Check((ui.Layout.PlaceButton.width > 0) == player.CanPlaceHeld, "Place button visible without a suitable surface.");
        Check((ui.Layout.RotateButton.width > 0) == (player.HeldItem != null), "Rotate button visible with empty hands.");
        Check((ui.Layout.HearButton.width > 0) == player.CanIdentify, "Whole-object hearing button missing or duplicated.");
        Check((ui.Layout.PartButton.width > 0) == player.CanIdentifyPart, "Detail button does not match the focused part.");
        foreach (Rect rect in new[] { ui.Layout.InteractButton, ui.Layout.PlaceButton, ui.Layout.RotateButton, ui.Layout.HearButton, ui.Layout.PartButton })
            if (rect.width > 0) Check(ui.Layout.SafeArea.Contains(rect.min) && ui.Layout.SafeArea.Contains(rect.max - Vector2.one), "Active action button outside safe area.");
    }

    private static void TestObjectAndPartControls()
    {
        var focusedField = typeof(PlayerInteraction).GetField("focused", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var partField = typeof(PlayerInteraction).GetField("focusedPart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        object oldFocus = focusedField.GetValue(player), oldPart = partField.GetValue(player);
        var ui = player.GetComponent<MobileControlsUI>();
        var room = player.GetComponent<RoomExperience>();
        try
        {
            foreach (VocabularyPart part in UnityEngine.Object.FindObjectsByType<VocabularyPart>())
            {
                KitchenInteractable owner = part.Owner;
                if (owner == null || owner.DisplayName == part.DisplayName) continue;
                focusedField.SetValue(player, owner); partField.SetValue(player, part);
                ui.RefreshLayout(); CheckContextualButtons(ui);
                Check(!ui.Layout.HearButton.Overlaps(ui.Layout.PartButton), "Object and detail circles overlap.");
                if (player.CanUseFocused)
                    Check(!ui.Layout.InteractButton.Overlaps(ui.Layout.HearButton) && !ui.Layout.InteractButton.Overlaps(ui.Layout.PartButton),
                        "Use/open circle overlaps hearing controls.");
                Check(room.TryPointer(ui.Layout.HearButton.center), "Object hearing touch was not consumed.");
                Check(player.IdentifiedName == owner.DisplayName, "Anhören named the part before its owner: " + part.name);
                Check(room.TryPointer(ui.Layout.PartButton.center), "Detail touch was not consumed.");
                Check(player.IdentifiedName == part.DisplayName && player.Voice.LastRequest.Length == 1 && player.Voice.LastRequest[0] == part.DisplayName,
                    "Detail touch did not speak the exact part alone: " + part.name);
            }
            Debug.Log("OBJECT_PART_CONTROLS_PASS: real part owners, primary names, detail-only speech, circle separation and touch routing.");
        }
        finally
        { focusedField.SetValue(player, oldFocus); partField.SetValue(player, oldPart); ui.RefreshLayout(); }
    }

    private static void TestExperience()
    {
        RoomExperience room = player.GetComponent<RoomExperience>();
        room.SetMenu(true);
        Check(!controls.GameplayActive && Time.timeScale == 0 && !player.GetComponent<MobileControlsUI>().IsVisible,
            "Pause did not suspend gameplay and hide touch controls.");
        room.SetMenu(false);
        Check(controls.GameplayActive && Time.timeScale > 0, "Resume did not restore gameplay.");
        BackgroundMusic music = player.GetComponent<BackgroundMusic>();
        bool enabledBefore = music.MusicEnabled;
        AudioSource voiceSource = player.Voice.GetComponent<AudioSource>();
        bool voiceMute = voiceSource.mute;
        music.Toggle();
        Check(music.MusicEnabled != enabledBefore && voiceSource.mute == voiceMute, "Music toggle changed narration or failed to toggle.");
        music.Toggle();
        Check(music.MusicEnabled == enabledBefore, "Music setting failed to restore.");
        foreach (KitchenInteractable item in UnityEngine.Object.FindObjectsByType<KitchenInteractable>())
        {
            Check(!System.Text.RegularExpressions.Regex.IsMatch(item.DisplayName, @"\d|\b(Left|Right|Prep)\b"), "Technical suffix in vocabulary: " + item.DisplayName);
            Check(Resources.Load<AudioClip>("Voice/" + GermanVoice.Key(item.DisplayName)) != null, "Missing pronunciation: " + item.DisplayName);
        }
    }

    private static void TestTouchGestures()
    {
        PostureGesture gesture = new PostureGesture();
        gesture.Begin(new Vector2(800, 500));
        Check(gesture.Sample(new Vector2(800, 400), 0, 72) == 0, "Swipe changed posture without a hold.");
        Check(gesture.Sample(new Vector2(800, 400), .61f, 72) == -1, "Downward swipe-and-hold not recognized.");
        Check(gesture.Sample(new Vector2(800, 300), 2, 72) == 0, "A single hold changed two postures.");
        gesture.Begin(new Vector2(800, 300));
        gesture.Sample(new Vector2(800, 400), 0, 72);
        Check(gesture.Sample(new Vector2(800, 400), .61f, 72) == 1, "Upward swipe-and-hold not recognized.");
        gesture.Reset();
        Check(gesture.Sample(new Vector2(800, 100), 10, 72) == 0, "Cancelled gesture changed posture.");
        gesture.Begin(new Vector2(800, 400));
        Check(gesture.Sample(new Vector2(950, 380), 1, 72) == 0, "Horizontal look was interpreted as a posture gesture.");
        TouchControlLayout layout = new TouchControlLayout {
            SafeArea = new Rect(0, 0, 1280, 720), JoystickCenter = new Vector2(145, 135), JoystickRadius = 80,
            InteractButton = new Rect(1097, 72, 116, 116), PlaceButton = new Rect(1097, 207, 116, 116),
            RotateButton = new Rect(957, 72, 116, 116)
        };
        TouchInputState state = new TouchInputState();
        state.Press(7, layout.JoystickCenter, layout, true, true);
        state.Drag(7, layout.JoystickCenter + Vector2.one * 4);
        Check(state.Movement == Vector2.zero, "Joystick dead zone causes drift.");
        state.Drag(7, layout.JoystickCenter + Vector2.one * 100);
        Check(state.Movement.x > .7f && state.Movement.y > .7f && state.Movement.magnitude <= 1.0001f,
            "Diagonal joystick movement is not normalized.");
        state.Press(21, new Vector2(800, 400), layout, true, true);
        state.Drag(21, new Vector2(860, 375));
        state.Press(99, layout.InteractButton.center, layout, true, true);
        Check(state.LookDelta == new Vector2(60, -25) && state.Movement.magnitude > .99f &&
              state.Actions == PlayerAction.Interact, "Simultaneous move/look/interact fingers interfere.");
        state.BeginFrame();
        Check(state.LookDelta == Vector2.zero && state.Actions == PlayerAction.None && state.Movement.magnitude > .99f,
            "Touch actions repeat or stationary movement stops between frames.");
        state.Drag(99, new Vector2(900, 450));
        Check(state.LookDelta == Vector2.zero, "Dragging off a button steals the look finger.");
        state.Release(99);
        state.Release(7);
        Check(state.Movement == Vector2.zero, "Released joystick keeps moving.");
        state.Drag(21, new Vector2(880, 385));
        Check(state.LookDelta == new Vector2(20, 10), "Releasing another finger interrupts look.");
        state.Press(99, layout.RotateButton.center, layout, true, false);
        state.Press(98, layout.PlaceButton.center, layout, true, false);
        Check(state.Actions == PlayerAction.None, "Disabled item buttons trigger actions.");
        state.Press(97, layout.RotateButton.center, layout, true, true);
        state.Press(96, layout.PlaceButton.center, layout, true, true);
        Check(state.Actions == (PlayerAction.Place | PlayerAction.Rotate), "Place/rotate touch buttons do not trigger.");
        state.Reset();
        Check(state.Movement == Vector2.zero && state.LookDelta == Vector2.zero && state.Actions == PlayerAction.None,
            "Focus loss/cancellation retains input.");
        state.Press(1, new Vector2(-10, 100), layout, true, true);
        state.Drag(1, new Vector2(900, 400));
        Check(state.Movement == Vector2.zero && state.LookDelta == Vector2.zero, "Touch outside safe area captures input.");
    }

    private static void TestPickupAndPlacement()
    {
        PickupItem plate = GameObject.Find("Teller_Plate").GetComponent<PickupItem>();
        PickupItem fork = GameObject.Find("Gabel_Fork").GetComponent<PickupItem>();
        Vector3 originalPosition = plate.transform.position;
        Check(player.TryPickUp(plate) && plate.IsHeld, "Pickup failed.");
        foreach (Collider collider in plate.GetComponentsInChildren<Collider>())
            Check(!collider.enabled, "Carried item retains a collider.");
        Check(!player.TryPickUp(fork) && player.HeldItem == plate, "A second pickup replaced the held item.");

        Check(Physics.Raycast(new Vector3(4, 1, -1), Vector3.right, out RaycastHit wall, 2), "Test wall missing.");
        Check(!player.TryPlaceHeld(wall) && player.HeldItem == plate, "Vertical wall accepted a placement.");
        Collider table = GameObject.Find("TableTop").GetComponent<Collider>();
        Check(table.Raycast(new Ray(new Vector3(.8f, 2, 1.39f), Vector3.down), out RaycastHit occupied, 3), "Test table missing.");
        Check(player.TryPlaceHeld(occupied), "Tabletop rejected free placement.");
        Check(player.TryPickUp(plate), "Cannot pick up after tabletop placement.");
        Check(table.Raycast(new Ray(new Vector3(.98f, 2, .9f), Vector3.down), out RaycastHit edge, 3), "Test table edge missing.");
        Check(player.TryPlaceHeld(edge), "Table edge rejected free placement.");
        plate.transform.position = originalPosition;
        Check(player.TryPickUp(plate), "Cannot pick up after edge placement.");
        player.enabled = false;
        Check(!plate.IsHeld && player.HeldItem == null && Vector3.Distance(plate.transform.position, originalPosition) < .001f,
            "Disabling the interactor did not restore the carried object.");
        player.enabled = true;

        Check(player.TryPickUp(plate), "Repeated pickup failed.");
        Check(Physics.Raycast(new Vector3(1.4f, 3, -1.5f), Vector3.down, out RaycastHit floor, 5), "Test floor missing.");
        Check(player.TryPlaceHeld(floor) && !plate.IsHeld && player.HeldItem == null, "Clear flat surface rejected placement.");
        foreach (Collider collider in plate.GetComponentsInChildren<Collider>())
            Check(collider.enabled, "Placed item collider was not restored.");
        Check(player.TryPickUp(plate), "Placed object cannot be picked up again.");
        player.enabled = false;
        player.enabled = true;
    }

    private static void TestFaucetAndPlayerCollision()
    {
        Faucet faucet = UnityEngine.Object.FindFirstObjectByType<Faucet>();
        Check(faucet != null && faucet.WaterStream != null && !faucet.IsRunning && !faucet.WaterStream.activeSelf,
            "Faucet must start closed with a bound stream.");
        faucet.Interact(player);
        Check(faucet.IsRunning && faucet.WaterStream.activeSelf, "Faucet did not turn on.");
        faucet.Interact(player);
        Check(!faucet.IsRunning && !faucet.WaterStream.activeSelf, "Faucet did not turn off.");

        GameObject blocker = new GameObject("Validation_PlayerBlocker");
        blocker.transform.position = player.GetComponent<CharacterController>().bounds.center;
        BoxCollider box = blocker.AddComponent<BoxCollider>();
        box.size = Vector3.one * .3f;
        Physics.SyncTransforms();
        Check(player.OverlapsPlayer(new Collider[] { box }), "Moving-door player penetration detection failed.");
        UnityEngine.Object.Destroy(blocker);
    }

    private static void Finish(bool error)
    {
        failed = error;
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }
}
