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
