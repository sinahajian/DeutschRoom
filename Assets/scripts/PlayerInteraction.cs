using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2.7f;

    private CharacterController controller;
    private PlayerControls controls;
    private KitchenInteractable focused;
    private VocabularyPart focusedPart;
    private RaycastHit focusHit;
    private bool hasHit;
    private float placementYaw;
    private string message;
    private float messageUntil;
    private GUIStyle promptStyle;
    public LearningProgress Learning { get; private set; }

    public GermanVoice Voice { get; private set; }
    private string identifiedName;
    private float nameUntil;
    public string IdentifiedName => Time.unscaledTime < nameUntil ? identifiedName : "";

    public PickupItem HeldItem { get; private set; }
    public KitchenInteractable FocusedObject => focused;
    public KitchenInteractable IdentificationTarget => focusedPart != null && focusedPart.Owner != null ? focusedPart.Owner : focused;
    public bool CanIdentify => IdentificationTarget != null || focusedPart != null;
    public bool CanIdentifyPart => focusedPart != null && IdentificationTarget != null &&
        !string.IsNullOrEmpty(focusedPart.DisplayName) && focusedPart.DisplayName != IdentificationTarget.DisplayName;
    public bool CanUseFocused => CanInteract && !(focused is VocabularyObject);
    public bool CanInteract => focused != null &&
        (!(focused is PickupItem pickup) || (HeldItem == null && !pickup.IsHeld)) &&
        (!(focused is Openable door) || !door.IsMoving);
    public bool CanPlaceHeld => HeldItem != null && hasHit && focusHit.collider != null && focusHit.normal.y >= .25f;
    public string FeedbackText => Time.unscaledTime < messageUntil ? message : "";

    private void Awake()
    {
        Voice = GetComponent<GermanVoice>();
        if (Voice == null) Voice = gameObject.AddComponent<GermanVoice>();
        controller = GetComponent<CharacterController>();
        Learning = GetComponent<LearningProgress>();
        if (Learning == null) Learning = gameObject.AddComponent<LearningProgress>();
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
            focused = null; focusedPart = null;
            return;
        }

        Physics.SyncTransforms();
        hasHit = RaycastWorld(new Ray(playerCamera.position, playerCamera.forward), interactionDistance, out focusHit);
        focusedPart = hasHit ? focusHit.collider.GetComponentInParent<VocabularyPart>() : null;
        focused = hasHit ? focusHit.collider.GetComponentInParent<KitchenInteractable>() : null;
        if (!controls.UsesTouch && Input.GetKeyDown(KeyCode.F) && (focused != null || focusedPart != null))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) IdentifyFocusedPart();
            else IdentifyFocused();
        }
        if (!controls.UsesTouch && Input.GetKeyDown(KeyCode.Return))
            Learning.StartLessons();
        if (!controls.UsesTouch && Input.GetKeyDown(KeyCode.H))
            Learning.RevealReviewTarget();
        if (controls.InteractPressed && CanInteract)
        {
            focused.Interact(this);
        }
        if (controls.RotatePressed && HeldItem != null)
            placementYaw += 45f;
        if (controls.PlacePressed && HeldItem != null)
        {
            if (!hasHit || !TryPlaceHeld(focusHit))
                ShowMessage("Schau auf eine Oberfläche in deiner Nähe.");
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
        if (Learning.IsPlacementReference(item))
        {
            Voice.Speak("Dieser Gegenstand bleibt als Bezugspunkt auf dem Tisch.");
            return false;
        }
        if (!Learning.CanScenarioPickup(item))
        { ShowMessage(Learning.ScenarioSession.Status); return false; }
        if (item == null || HeldItem != null || item.IsHeld)
        {
            ShowMessage(controls != null && controls.UsesTouch ? "Lege zuerst den Gegenstand ab." : "Lege zuerst den Gegenstand ab (G).");
            return false;
        }
        placementYaw = item.transform.eulerAngles.y;
        bool takenFromTable = Learning.IsOnTable(item);
        HeldItem = item;
        item.BeginCarry();
        Learning.Found(item, takenFromTable);
        return true;
    }

    public bool TryPlaceHeld(RaycastHit surface)
    {
        if (HeldItem == null || surface.collider == null || surface.normal.y < .25f)
            return false;
        Quaternion rotation = HeldItem.PlacementRotation(placementYaw);
        Bounds rotated = HeldItem.BoundsAt(Vector3.zero, rotation);
        Vector3 position = surface.point - new Vector3(rotated.center.x, rotated.min.y, rotated.center.z) + Vector3.up * .01f;
        PickupItem placed = HeldItem;
        placed.PlaceOnSurface(position, rotation, surface.collider.transform);
        HeldItem = null;
        ShowMessage("Abgelegt.");
        Learning.Placed(placed);
        return true;
    }

    public void IdentifyFocused()
    {
        if (IdentificationTarget != null) Identify(IdentificationTarget);
        else if (focusedPart != null) IdentifyPart(focusedPart);
    }

    public void Identify(KitchenInteractable item)
    {
        if (item == null) return;
        identifiedName = item.DisplayName; nameUntil = Time.unscaledTime + 3.5f;
        if (!Learning.Identified(item)) Voice.SpeakIdentification(item.DisplayName);
    }

    public void IdentifyFocusedPart()
    {
        if (CanIdentifyPart) IdentifyPart(focusedPart);
    }

    public void IdentifyPart(VocabularyPart part)
    {
        if (part == null) return;
        identifiedName = part.DisplayName;
        nameUntil = Time.unscaledTime + 3.5f;
        // The detail control names only the part; whole-object learning belongs to Anhören.
        Voice.SpeakIdentification(part.DisplayName);
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

    public void CancelCarry()
    {
        if (HeldItem != null)
            HeldItem.ReturnToPreviousPose();
        HeldItem = null;
    }

    private void OnDisable() => CancelCarry();

    private void OnGUI()
    {
        var experience = GetComponent<RoomExperience>();
        if (experience != null && (experience.MenuOpen || experience.ModalOpen)) return;
        if (playerCamera == null || (controls != null && controls.UsesTouch))
            return;
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 17, wordWrap = true };
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            GUI.Box(new Rect(Screen.width / 2f - 230f, Screen.height - 100f, 460f, 60f),
                "Klicken zum Spielen | M: Touch-Steuerung\nWASD: Bewegen | Maus: Umsehen | Esc: Maus freigeben", promptStyle);
            return;
        }
        GUI.Label(new Rect(Screen.width / 2f - 5f, Screen.height / 2f - 12f, 20f, 24f), "+");
        string prompt = "E · Benutzen    G · Ablegen    R · Drehen    Esc · Menü";
        GUI.Box(new Rect(Screen.width / 2f - 290f, Screen.height - 65f, 580f, 45f), prompt, promptStyle);
    }
}
