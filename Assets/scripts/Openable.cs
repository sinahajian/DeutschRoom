using UnityEngine;

public sealed class Openable : KitchenInteractable
{
    [SerializeField] private bool sliding;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, -0.28f);
    [SerializeField, Min(0.1f)] private float animationDuration = 0.55f;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private Collider[] movingColliders;
    private PlayerInteraction operatorPlayer;
    private float progress;

    private bool lessonClearanceRequired;
    public uint BlockedByPlayerSequence { get; private set; }
    public void RequireLessonClearance(bool value) => lessonClearanceRequired = value;
    public Vector3 LessonControlPoint
    {
        get
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool first = true;
            foreach (var c in movingColliders)
            { if (!c.enabled) continue; if (first) { bounds = c.bounds; first = false; } else bounds.Encapsulate(c.bounds); }
            return bounds.center;
        }
    }
    public float LessonPlayerDistance(PlayerInteraction player)
    { Vector3 a = player.transform.position, b = LessonControlPoint; return Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z)); }
    public uint InteractionSequence { get; private set; }
    public void SetLessonState(bool open)
    {
        operatorPlayer = null; IsOpen = open; progress = open ? 1 : 0;
        ApplyPose(); Physics.SyncTransforms();
    }
    public bool IsOpen { get; private set; }
    public bool IsMoving => !Mathf.Approximately(progress, IsOpen ? 1f : 0f);

    private void Awake()
    {
        closedPosition = transform.localPosition;
        closedRotation = transform.localRotation;
        movingColliders = GetComponentsInChildren<Collider>();
    }

    public override string GetAction(PlayerInteraction player) => IsOpen ? "Schließen" : "Öffnen";

    public override void Interact(PlayerInteraction player)
    {
        InteractionSequence = unchecked(InteractionSequence + 1);
        if (lessonClearanceRequired && player != null && LessonScenarioRules.BlockOpening(true, IsOpen, LessonPlayerDistance(player)))
        {
            BlockedByPlayerSequence = unchecked(BlockedByPlayerSequence + 1);
            player.ShowMessage("Tritt etwas zurück, damit die Tür genug Platz hat.");
            player.Voice.Speak("Tritt etwas zurück, damit die Tür genug Platz hat.");
            return;
        }
        operatorPlayer = player;
        IsOpen = !IsOpen;
    }

    private void Update()
    {
        if (!IsMoving)
            return;

        float previous = progress;
        progress = Mathf.MoveTowards(progress, IsOpen ? 1f : 0f, Time.deltaTime / animationDuration);
        ApplyPose();

        // Check each thin door part, rather than a large enclosing box around the swing.
        if (operatorPlayer != null && operatorPlayer.OverlapsPlayer(movingColliders))
        {
            BlockedByPlayerSequence = unchecked(BlockedByPlayerSequence + 1);
            progress = previous;
            ApplyPose();
            IsOpen = !IsOpen;
            operatorPlayer.ShowMessage("Geh einen Schritt zurück, damit die Tür Platz hat.");
        }
        Physics.SyncTransforms();
    }

    private void ApplyPose()
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        transform.localPosition = closedPosition + (sliding ? slideOffset * eased : Vector3.zero);
        transform.localRotation = closedRotation *
            (sliding ? Quaternion.identity : Quaternion.AngleAxis(openAngle * eased, rotationAxis.normalized));
    }
}
