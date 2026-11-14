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
    private bool mouseHeld;
    private Rect touchSafeArea;

    public bool UsesTouch { get; private set; }
    public bool GameplayActive => hasFocus && !suspended &&
        (UsesTouch || Cursor.lockState == CursorLockMode.Locked);
    public Vector2 Movement { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool PlacePressed { get; private set; }
    public bool RotatePressed { get; private set; }
    public Vector2 JoystickValue => touches.Movement;
    public static Rect ModeButtonRect => new Rect(16, 16, 190, 44);

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        overlay = GetComponent<MobileControlsUI>();
        overlay.Initialize(this);
        ApplyMode();
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
        if (overlay != null && overlay.IsVisible != UsesTouch)
            overlay.SetVisible(UsesTouch);
    }

    private void Update()
    {
        touches.BeginFrame();
        Movement = LookDelta = Vector2.zero;
        InteractPressed = PlacePressed = RotatePressed = false;

        if (Input.touchCount > 0 && !detectedTouch)
        {
            detectedTouch = true;
            ApplyMode();
        }
        ApplyMode();
        if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.M))
            SetMode(UsesTouch ? ControlMode.KeyboardAndMouse : ControlMode.Touch);

        if (!hasFocus || suspended)
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
        }
    }

    private void ReadKeyboardAndMouse()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 guiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (Cursor.lockState != CursorLockMode.Locked && ModeButtonRect.Contains(guiPosition))
            {
                SetMode(ControlMode.Touch);
                return;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (!GameplayActive)
            return;
        Movement = Vector2.ClampMagnitude(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")), 1f);
        LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        InteractPressed = Input.GetKeyDown(KeyCode.E);
        PlacePressed = Input.GetKeyDown(KeyCode.G);
        RotatePressed = Input.GetKeyDown(KeyCode.R);
    }

    private void ReadTouch()
    {
        overlay.RefreshLayout();
        TouchControlLayout layout = overlay.Layout;
        if (touchSafeArea != layout.SafeArea)
        {
            touches.Reset();
            mouseHeld = false;
            touchSafeArea = layout.SafeArea;
        }
        bool canInteract = interaction != null && interaction.FocusedObject != null;
        bool isHolding = interaction != null && interaction.HeldItem != null;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touches.Press(touch.fingerId, touch.position, layout, canInteract, isHolding);
                    break;
                case TouchPhase.Moved:
                    touches.Drag(touch.fingerId, touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
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
                mouseHeld = true;
                touches.Press(-100, position, layout, canInteract, isHolding);
            }
            else if (mouseHeld && Input.GetMouseButton(0))
                touches.Drag(-100, position);
            if (Input.GetMouseButtonUp(0))
            {
                touches.Release(-100);
                mouseHeld = false;
            }
        }
        Movement = touches.Movement;
        // Touch delta is displacement, so it must not be multiplied by deltaTime.
        LookDelta = touches.LookDelta * (touchLookSensitivity * 720f / Mathf.Max(1, Mathf.Min(Screen.width, Screen.height)));
    }

    private void ApplyCursor()
    {
        Cursor.lockState = UsesTouch ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = true;
        if (!UsesTouch)
            Cursor.visible = false;
    }

    public void ResetInput()
    {
        touches.Reset();
        queuedActions = PlayerAction.None;
        Movement = LookDelta = Vector2.zero;
        InteractPressed = PlacePressed = RotatePressed = false;
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
            overlay.SetVisible(UsesTouch);
    }
}
