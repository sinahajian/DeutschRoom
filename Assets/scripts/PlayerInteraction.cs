using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2.7f;

    private CharacterController controller;
    private PlayerControls controls;
    private KitchenInteractable focused;
    private RaycastHit focusHit;
    private bool hasHit;
    private float placementYaw;
    private string message;
    private float messageUntil;
    private GUIStyle promptStyle;

    public PickupItem HeldItem { get; private set; }
    public KitchenInteractable FocusedObject => focused;
    public string FeedbackText => Time.unscaledTime < messageUntil ? message : "";

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = GetComponent<PlayerControls>();
        if (playerCamera == null)
        {
            Camera cameraComponent = GetComponentInChildren<Camera>();
            if (cameraComponent != null)
                playerCamera = cameraComponent.transform;
        }
        if (playerCamera == null)
            enabled = false;
    }

    private void Update()
    {
        if (controls == null || !controls.GameplayActive)
        {
            focused = null;
            return;
        }

        Physics.SyncTransforms();
        hasHit = RaycastWorld(new Ray(playerCamera.position, playerCamera.forward), interactionDistance, out focusHit);
        focused = hasHit ? focusHit.collider.GetComponentInParent<KitchenInteractable>() : null;
        if (controls.InteractPressed && focused != null)
            focused.Interact(this);
        if (controls.RotatePressed && HeldItem != null)
            placementYaw += 45f;
        if (controls.PlacePressed && HeldItem != null)
        {
            if (!hasHit || !TryPlaceHeld(focusHit))
                ShowMessage("Look at a clear, flat surface with room for the item.");
        }
    }

    private void LateUpdate()
    {
        if (HeldItem == null || playerCamera == null)
            return;
        Quaternion rotation = playerCamera.rotation * Quaternion.Euler(0f, placementYaw - transform.eulerAngles.y, 0f);
        Vector3 offset = playerCamera.TransformVector(new Vector3(.22f, -.19f, .78f));
        float radius = Mathf.Max(.04f, HeldItem.LocalBounds.extents.magnitude);
        if (Physics.SphereCast(playerCamera.position, radius, offset.normalized, out RaycastHit obstacle,
            offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            !obstacle.transform.IsChildOf(transform))
            offset = offset.normalized * Mathf.Max(.12f, obstacle.distance - .03f);
        Vector3 center = playerCamera.position + offset;
        HeldItem.SetCarriedPose(center - rotation * HeldItem.LocalBounds.center, rotation);
    }

    public bool TryPickUp(PickupItem item)
    {
        if (item == null || HeldItem != null || item.IsHeld)
        {
            ShowMessage(controls != null && controls.UsesTouch ? "Place your item first." : "Put down your item first (G).");
            return false;
        }
        placementYaw = item.transform.eulerAngles.y;
        HeldItem = item;
        item.BeginCarry();
        return true;
    }

    public bool TryPlaceHeld(RaycastHit surface)
    {
        if (HeldItem == null || surface.collider == null || surface.normal.y < .85f)
            return false;
        // Moving doors and other loose items are not stable supports.
        if (surface.collider.GetComponentInParent<KitchenInteractable>() != null)
            return false;

        Quaternion rotation = HeldItem.PlacementRotation(placementYaw);
        Bounds rotated = HeldItem.BoundsAt(Vector3.zero, rotation);
        Vector3 position = surface.point - new Vector3(rotated.center.x, rotated.min.y, rotated.center.z) + Vector3.up * .006f;
        Bounds bounds = HeldItem.BoundsAt(position, rotation);

        Collider[] obstacles = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider obstacle in obstacles)
            if (!obstacle.transform.IsChildOf(HeldItem.transform))
                return false;

        // Sample the footprint to keep plates, boards and pans from hanging off a counter.
        for (int i = 0; i < 5; i++)
        {
            Vector3 probe = bounds.center;
            if (i > 0)
            {
                probe.x += ((i & 1) == 0 ? -1f : 1f) * bounds.extents.x * .9f;
                probe.z += i <= 2 ? -bounds.extents.z * .9f : bounds.extents.z * .9f;
            }
            probe.y = bounds.min.y + .025f;
            if (!RaycastWorld(new Ray(probe, Vector3.down), .075f, out RaycastHit support) ||
                support.normal.y < .85f || support.collider.GetComponentInParent<KitchenInteractable>() != null)
                return false;
        }

        HeldItem.Place(position, rotation);
        HeldItem = null;
        ShowMessage("Placed.");
        return true;
    }

    public bool OverlapsPlayer(Collider[] movingParts)
    {
        if (controller == null)
            return false;
        foreach (Collider part in movingParts)
        {
            if (part.enabled && Physics.ComputePenetration(part, part.transform.position, part.transform.rotation,
                controller, controller.transform.position, controller.transform.rotation, out _, out float distance) && distance > .002f)
                return true;
        }
        return false;
    }

    public void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 2.5f;
    }

    private bool RaycastWorld(Ray ray, float distance, out RaycastHit closest)
    {
        closest = default;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) ||
                (HeldItem != null && hit.collider.transform.IsChildOf(HeldItem.transform)))
                continue;
            if (hit.distance < nearest)
            {
                closest = hit;
                nearest = hit.distance;
            }
        }
        return nearest < float.PositiveInfinity;
    }

    private void OnDisable()
    {
        if (HeldItem != null)
            HeldItem.ReturnToPreviousPose();
        HeldItem = null;
    }

    private void OnGUI()
    {
        if (playerCamera == null || (controls != null && controls.UsesTouch))
            return;
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 17, wordWrap = true };
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            GUI.Box(new Rect(Screen.width / 2f - 230f, Screen.height - 100f, 460f, 60f),
                "Click to play | M: touch controls\nWASD: move | Mouse: look | Esc: release cursor", promptStyle);
            return;
        }
        GUI.Label(new Rect(Screen.width / 2f - 5f, Screen.height / 2f - 12f, 20f, 24f), "+");
        string prompt = focused != null ? "E - " + focused.GetAction(this) + " | " + focused.DisplayName : "Look at a door, drawer, item or faucet";
        if (HeldItem != null)
            prompt += "\nG - Place | R - Rotate | " + HeldItem.DisplayName;
        if (Time.unscaledTime < messageUntil)
            prompt += "\n" + message;
        GUI.Box(new Rect(Screen.width / 2f - 290f, Screen.height - 112f, 580f, 94f), prompt, promptStyle);
    }
}
