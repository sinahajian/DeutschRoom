using UnityEngine;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(MobileControlsUI))]
public sealed class PlayerControls : MonoBehaviour
{
    public enum ControlMode { Automatic, KeyboardAndMouse, Touch }

    [SerializeField] private ControlMode controlMode;
    [SerializeField, Range(.02f, .5f)] private float touchLookSensitivity = .15f;

    private readonly TouchInputState touches = new TouchInputState();
    private MobileControlsUI overlay;
    private PlayerInteraction interaction;
    private PlayerAction queuedActions;
    private bool detectedTouch;
    private bool hasFocus = true;
    private bool suspended;
    private bool menuOpen;
    private RoomExperience experience;
    private bool mouseHeld;
    private bool practicePointerUnlocked;
    private Rect touchSafeArea;

    private int namingFinger=-1;
    private Vector2 namingPress;
    private float namingPressedAt;
    private bool EligibleNameTap(Vector2 at, TouchControlLayout layout) =>
        !layout.InteractButton.Contains(at) && !layout.PlaceButton.Contains(at) && !layout.RotateButton.Contains(at) &&
        !layout.HearButton.Contains(at) && !layout.PartButton.Contains(at) &&
        Vector2.Distance(at,layout.JoystickCenter)>layout.JoystickRadius*1.4f;
    public bool UsesTouch { get; private set; }
    public bool GameplayActive => hasFocus && !suspended && !menuOpen &&
        (experience == null || !experience.MapExpanded && !experience.ModalOpen) &&
        (UsesTouch || Cursor.lockState == CursorLockMode.Locked);
    public Vector2 Movement { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool PlacePressed { get; private set; }
    public bool RotatePressed { get; private set; }
    public bool LowerPosturePressed { get; private set; }
    public bool RaisePosturePressed { get; private set; }
    public Vector2 JoystickValue => touches.Movement;
    public static Rect ModeButtonRect => new Rect(16, 16, 190, 44);

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        overlay = GetComponent<MobileControlsUI>();
        overlay.Initialize(this);
        experience = GetComponent<RoomExperience>();
        if (experience == null) experience = gameObject.AddComponent<RoomExperience>();
        SetMenuOpen(true);
        ApplyMode();
    }

    public void SetMenuOpen(bool value)
    {
        menuOpen = value; ResetInput(); ApplyCursor();
        if (overlay != null) overlay.SetVisible(UsesTouch && !menuOpen && (experience == null || !experience.MapExpanded && !experience.ModalOpen));
    }

    private void Start()
    {
        ApplyCursor();
    }

    public void SetMode(ControlMode mode)
    {
        controlMode = mode;
        ApplyMode();
    }

    public void RequestAction(PlayerAction action)
    {
        queuedActions |= action;
    }

    private void ApplyMode()
    {
        bool useTouch = controlMode == ControlMode.Touch ||
            (controlMode == ControlMode.Automatic && (Application.isMobilePlatform || detectedTouch));
        bool changed = useTouch != UsesTouch;
        UsesTouch = useTouch;
        if (changed)
        {
            ResetInput();
            ApplyCursor();
        }
        if (overlay != null && overlay.IsVisible != (UsesTouch && !menuOpen && (experience == null || !experience.MapExpanded && !experience.ModalOpen)))
            overlay.SetVisible(UsesTouch && !menuOpen && (experience == null || !experience.MapExpanded && !experience.ModalOpen));
    }

    private void Update()
    {
        touches.BeginFrame();
        Movement = LookDelta = Vector2.zero;
        InteractPressed = PlacePressed = RotatePressed = LowerPosturePressed = RaisePosturePressed = false;

        if (Input.touchCount > 0 && !detectedTouch)
        {
            detectedTouch = true;
            ApplyMode();
        }
        ApplyMode();
        if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.M))
            SetMode(UsesTouch ? ControlMode.KeyboardAndMouse : ControlMode.Touch);

        if (!hasFocus || suspended || menuOpen)
        {
            ResetInput();
            return;
        }
        if (UsesTouch)
            ReadTouch();
        else
            ReadKeyboardAndMouse();

        PlayerAction actions = touches.Actions | queuedActions;
        queuedActions = PlayerAction.None;
        if (GameplayActive)
        {
            InteractPressed |= (actions & PlayerAction.Interact) != 0;
            PlacePressed |= (actions & PlayerAction.Place) != 0;
            RotatePressed |= (actions & PlayerAction.Rotate) != 0;
            LowerPosturePressed |= (actions & PlayerAction.LowerPosture) != 0;
            RaisePosturePressed |= (actions & PlayerAction.RaisePosture) != 0;
        }
    }

    private void ReadKeyboardAndMouse()
    {
        bool practicePointer = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (practicePointer)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else if (practicePointerUnlocked) ApplyCursor();
        practicePointerUnlocked = practicePointer;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 pointer = Cursor.lockState == CursorLockMode.Locked ? new Vector2(Screen.width / 2f, Screen.height / 2f) : (Vector2)Input.mousePosition;
            if (experience.TryPointer(pointer)) return;
            interaction.TryIdentifyAtScreen(pointer);
            if (!practicePointer)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        if (!GameplayActive)
            return;
        Movement = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1f);
        LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        InteractPressed = Input.GetKeyDown(KeyCode.E);
        PlacePressed = Input.GetKeyDown(KeyCode.G);
        RotatePressed = Input.GetKeyDown(KeyCode.R);
        LowerPosturePressed = Input.GetKeyDown(KeyCode.C);
        RaisePosturePressed = Input.GetKeyDown(KeyCode.X);
    }

    private void ReadTouch()
    {
        overlay.RefreshLayout();
        TouchControlLayout layout = overlay.Layout;
        if (touchSafeArea != layout.SafeArea)
        {
            touches.Reset();
            namingFinger=-1;
            mouseHeld = false;
            touchSafeArea = layout.SafeArea;
        }
        bool canInteract = interaction != null && interaction.CanInteract;
        bool isHolding = interaction != null && interaction.HeldItem != null;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (experience.TryPointer(touch.position)) break;
                    if (namingFinger<0 && EligibleNameTap(touch.position,layout))
                    { namingFinger=touch.fingerId;namingPress=touch.position;namingPressedAt=Time.unscaledTime; }
                    overlay.ShowPress(touch.position);
                    touches.Press(touch.fingerId, touch.position, layout, canInteract, isHolding);
                    break;
                case TouchPhase.Moved:
                    if (touch.fingerId==namingFinger && Vector2.Distance(touch.position,namingPress)>18) namingFinger=-1;
                    touches.Drag(touch.fingerId, touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId==namingFinger)
                    {
                        if(ObjectNameTapRules.Accepts(Time.unscaledTime-namingPressedAt,Vector2.Distance(touch.position,namingPress),touch.phase==TouchPhase.Ended))
                            interaction.TryIdentifyAtScreen(touch.position);
                        namingFinger=-1;
                    }
                    touches.Release(touch.fingerId);
                    break;
            }
        }
        // Mouse preview in the editor/desktop build. Real touch never feeds this path.
        if (!Application.isMobilePlatform && !detectedTouch)
        {
            Vector2 position = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (experience.TryPointer(position)) return;
                mouseHeld = true;
                namingFinger=EligibleNameTap(position,layout)?-100:-1;namingPress=position;namingPressedAt=Time.unscaledTime;
                overlay.ShowPress(position);
                touches.Press(-100, position, layout, canInteract, isHolding);
            }
            else if (mouseHeld && Input.GetMouseButton(0))
            {
                if(namingFinger==-100 && Vector2.Distance(position,namingPress)>18)namingFinger=-1;
                touches.Drag(-100, position);
            }
            if (Input.GetMouseButtonUp(0))
            {
                if(namingFinger==-100 && ObjectNameTapRules.Accepts(Time.unscaledTime-namingPressedAt,Vector2.Distance(position,namingPress),true)) interaction.TryIdentifyAtScreen(position);
                namingFinger=-1;
                touches.Release(-100);
                mouseHeld = false;
            }
        }
        touches.SamplePosture(Time.unscaledTime, 72f * Mathf.Max(.6f, Mathf.Min(Screen.width, Screen.height) / 720f));
        Movement = touches.Movement;
        // Touch delta is displacement, so it must not be multiplied by deltaTime.
        LookDelta = touches.LookDelta * (touchLookSensitivity * 720f / Mathf.Max(1, Mathf.Min(Screen.width, Screen.height)));
    }

    public void RefreshContext()
    {
        ResetInput(); ApplyCursor();
        if (overlay != null) overlay.SetVisible(UsesTouch && !menuOpen && (experience == null || !experience.MapExpanded && !experience.ModalOpen));
    }

    private void ApplyCursor()
    {
        bool modal = experience != null && experience.ModalOpen;
        Cursor.lockState = UsesTouch || menuOpen || modal ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = true;
        if (!UsesTouch && !menuOpen && !modal)
            Cursor.visible = false;
    }

    public void ResetInput()
    {
        touches.Reset();
        namingFinger=-1;
        queuedActions = PlayerAction.None;
        Movement = LookDelta = Vector2.zero;
        InteractPressed = PlacePressed = RotatePressed = LowerPosturePressed = RaisePosturePressed = false;
        mouseHeld = false;
    }

    private void OnApplicationFocus(bool value)
    {
        hasFocus = value;
        ResetInput();
        if (!value && !UsesTouch)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnApplicationPause(bool value)
    {
        suspended = value;
        ResetInput();
    }

    private void OnDisable()
    {
        ResetInput();
        if (overlay != null)
            overlay.SetVisible(false);
    }

    private void OnEnable()
    {
        if (overlay != null)
            overlay.SetVisible(UsesTouch && !menuOpen && (experience == null || !experience.MapExpanded && !experience.ModalOpen));
    }
}

// A tap names an object; a look gesture, hold or cancellation must not name it.
public static class ObjectNameTapRules
{
    public static bool Accepts(float elapsed,float travel,bool released) => released && elapsed>=0 && elapsed<.5f && travel>=0 && travel<=18;
}
