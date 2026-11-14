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
                controls = player.GetComponent<PlayerControls>();
                Check(controls != null, "Shared desktop/touch controls missing.");
                TestTouchGestures();
                doors = UnityEngine.Object.FindObjectsByType<Openable>(FindObjectsSortMode.None);
                PickupItem[] items = UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
                Check(player != null && player.enabled, "Player interaction/camera binding missing.");
                Check(doors.Length == 20, "Expected 20 doors and drawers.");
                Check(items.Length == 17, "Expected 17 portable objects.");
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
                Check(ui.Layout.SafeArea.Contains(ui.Layout.InteractButton.center) &&
                      ui.Layout.SafeArea.Contains(ui.Layout.PlaceButton.center) &&
                      ui.Layout.SafeArea.Contains(ui.Layout.RotateButton.center), "Touch buttons outside safe area.");
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
                cameraTransform.LookAt(new Vector3(2, 0, -1.6f));
                controls.RequestAction(PlayerAction.Rotate | PlayerAction.Place);
                stage = 6;
                deadline = EditorApplication.timeSinceStartup + .25;
                return;
            }
            Check(player.HeldItem == null && !touchPlate.IsHeld, "Touch place action did not place the carried plate.");
            Check(Mathf.Abs(Mathf.DeltaAngle(touchPlate.transform.eulerAngles.y, 45)) < .1f,
                "Touch rotate action did not rotate the placed item.");
            controls.SetMode(PlayerControls.ControlMode.KeyboardAndMouse);
            Check(!controls.UsesTouch && !player.GetComponent<MobileControlsUI>().IsVisible,
                "Desktop mode did not hide the touch overlay.");
            Debug.Log("INTERACTION_VALIDATION_PASS: 20 doors/drawers, 17 pickup bounds, placement rejection/restoration, faucet and player collision; multitouch ownership/dead zone/cancellation; touch-mode door, pickup, rotate and placement actions with cursor unlocked; desktop/touch switching.");
            Finish(false);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Finish(true);
        }
    }

    private static void TestTouchGestures()
    {
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
        Check(!player.TryPlaceHeld(occupied), "Occupied tabletop accepted a placement.");
        Check(table.Raycast(new Ray(new Vector3(.98f, 2, .9f), Vector3.down), out RaycastHit edge, 3), "Test table edge missing.");
        Check(!player.TryPlaceHeld(edge), "Unsupported overhang accepted a placement.");
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
