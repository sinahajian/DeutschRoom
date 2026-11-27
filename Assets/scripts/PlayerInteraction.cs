using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2.7f;

    private readonly RaycastHit[] worldHits = new RaycastHit[32];
    private CharacterController controller;
    private PlayerControls controls;
    private RoomExperience experience;
    private HandsOnTutorial Practice => experience != null && experience.HandsOnActive ? experience.HandsOn : null;
    public uint RotationSequence { get; private set; }
    private float pickupElapsed = 1, placeElapsed, releaseElapsed = 1;
    private Vector3 pickupStart, placeStart, placeLocalPosition, placeLocalNormal, releasePosition;
    private Quaternion pickupStartRotation, placeStartRotation, placeLocalRotation;
    private Collider placementCollider;
    private Transform placementSurface;
    private bool placing;
    public bool IsPlacing => placing;
    public bool CanRotateHeld => HeldItem != null && !placing;
    public bool HandEngaged => HeldItem != null || releaseElapsed < .24f;
    public float HandBlend => HeldItem==null ? 1-ManipulationMotion.Ease(releaseElapsed/.24f) : placing ? 1 : ManipulationMotion.Ease(pickupElapsed/.16f);
    public float HandClosure => HeldItem == null ? 0 : placing ?
        1-ManipulationMotion.Ease((placeElapsed/ManipulationMotion.PlaceSeconds-.5f)*2) : ManipulationMotion.Ease((pickupElapsed-.10f)/.18f);
    public Vector3 HandPosition
    {
        get
        {
            if(HeldItem==null)return releasePosition;
            return HeldItem.transform.TransformPoint(HeldItem.LocalGripPoint);
        }
    }
    public Quaternion HandRotation => HeldItem != null ? HeldItem.transform.rotation : releaseRotation;
    private Quaternion releaseRotation=Quaternion.identity;

    // The imported palm is the visual anchor only after pickup settles. Placement
    // keeps its validated surface pose and animation path as its sole authority.
    public void AlignSettledCarryToPalm(Vector3 palm)
    {
        if(HeldItem==null || placing || pickupElapsed<ManipulationMotion.PickupSeconds)return;
        HeldItem.SetCarriedPose(HeldItem.transform.position+palm-HandPosition,HeldItem.transform.rotation);
    }
    internal void RestorePracticeRotation(uint sequence) => RotationSequence = sequence;
    internal void ClearPracticeFeedback() { identifiedName = message = ""; nameUntil = messageUntil = 0; }
    internal float PracticeCarryYaw => placementYaw;
    internal void ResumePracticeCarry(PickupItem item, float yaw, PickupItem.PracticeInteractionState state)
    {
        HeldItem = item; placementYaw = yaw;
        item.BeginCarry(); item.RestorePracticeInteractions(state);
        pickupElapsed=ManipulationMotion.PickupSeconds; releaseElapsed=1;
    }
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
    private bool identifiedByPointer;
    public bool PointerNameVisible => identifiedByPointer && Time.unscaledTime < nameUntil;
    public string IdentifiedName => Time.unscaledTime < nameUntil ? identifiedName : "";

    public PickupItem HeldItem { get; private set; }
    public KitchenInteractable FocusedObject => focused;
    public KitchenInteractable IdentificationTarget => focusedPart != null && focusedPart.Owner != null ? focusedPart.Owner : focused;
    public bool CanIdentify => !placing && (IdentificationTarget != null || focusedPart != null) &&
        (Practice == null || Practice.AllowsIdentification(IdentificationTarget));
    public bool CanIdentifyPart => !placing && Practice == null && focusedPart != null && IdentificationTarget != null &&
        !string.IsNullOrEmpty(focusedPart.DisplayName) && focusedPart.DisplayName != IdentificationTarget.DisplayName;
    public bool CanUseFocused => CanInteract && !(focused is VocabularyObject);
    public bool CanInteract => !placing && focused != null &&
        (Practice == null || Practice.AllowsPickup(focused)) &&
        (!(focused is PickupItem pickup) || (HeldItem == null && !pickup.IsHeld)) &&
        (!(focused is Openable door) || !door.IsMoving);
    public bool CanPlaceHeld => !placing && HeldItem != null && hasHit && focusHit.collider != null && focusHit.normal.y >= .25f &&
        (Practice == null || Practice.AllowsPlacement(focusHit.collider.transform));
    public string FeedbackText => Time.unscaledTime < messageUntil ? message : "";

    private void Awake()
    {
        Voice = GetComponent<GermanVoice>();
        if (Voice == null) Voice = gameObject.AddComponent<GermanVoice>();
        controller = GetComponent<CharacterController>();
        Learning = GetComponent<LearningProgress>();
        if (Learning == null) Learning = gameObject.AddComponent<LearningProgress>();
        controls = GetComponent<PlayerControls>();
        experience = GetComponent<RoomExperience>();
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
        if (controls.RotatePressed && CanRotateHeld && (Practice == null || Practice.AllowsRotation))
        { placementYaw += 45f; RotationSequence = unchecked(RotationSequence + 1); }
        if (controls.PlacePressed && HeldItem != null && !placing)
        {
            if (!hasHit || !TryPlaceHeld(focusHit))
                ShowMessage("Schau auf eine Oberfläche in deiner Nähe.");
        }
    }

    private void LateUpdate()
    {
        releaseElapsed+=Time.deltaTime;
        if (playerCamera == null) return;
        if(placing) {AdvancePlacement(Time.deltaTime);return;}
        if(HeldItem==null)return;
        pickupElapsed+=Time.deltaTime;
        Quaternion rotation = playerCamera.rotation * Quaternion.Euler(0f, placementYaw - transform.eulerAngles.y, 0f);
        Vector3 offset = playerCamera.TransformVector(new Vector3(.22f, -.32f, .43f));
        float radius = Mathf.Max(.04f, HeldItem.LocalBounds.extents.magnitude);
        if (Physics.SphereCast(playerCamera.position, radius, offset.normalized, out RaycastHit obstacle,
            offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            !obstacle.transform.IsChildOf(transform))
            offset = offset.normalized * Mathf.Max(.12f, obstacle.distance - .03f);
        Vector3 center = playerCamera.position + offset;
        float t=Mathf.Clamp01(pickupElapsed/ManipulationMotion.PickupSeconds), ease=ManipulationMotion.Ease(t);
        Vector3 target=center-rotation*HeldItem.LocalBounds.center;
        HeldItem.SetCarriedPose(Vector3.Lerp(pickupStart,target,ease)+Vector3.up*ManipulationMotion.Lift(t,.065f),
            Quaternion.Slerp(pickupStartRotation,rotation,ease));
    }

    public bool TryPickUp(PickupItem item)
    {
        if (Practice != null && !Practice.AllowsPickup(item)) return false;
        if (Practice == null && Learning.IsPlacementReference(item))
        {
            Voice.Speak("Dieser Gegenstand bleibt als Bezugspunkt auf dem Tisch.");
            return false;
        }
        if (Practice == null && !Learning.CanScenarioPickup(item))
        { ShowMessage(Learning.ScenarioSession.Status); return false; }
        if (item == null || HeldItem != null || item.IsHeld)
        {
            ShowMessage(controls != null && controls.UsesTouch ? "Lege zuerst den Gegenstand ab." : "Lege zuerst den Gegenstand ab (G).");
            return false;
        }
        placementYaw = item.transform.eulerAngles.y;
        pickupStart=item.transform.position;pickupStartRotation=item.transform.rotation;pickupElapsed=0;releaseElapsed=1;
        HeldItem = item;
        item.BeginCarry();
        Learning.Found(item);
        return true;
    }

    public bool TryPlaceHeld(RaycastHit surface)
    {
        if (placing || HeldItem == null || surface.collider == null || surface.normal.y < .25f)
            return false;
        if (Practice != null && !Practice.AllowsPlacement(surface.collider.transform)) return false;
        Quaternion rotation = HeldItem.PlacementRotation(placementYaw);
        Bounds rotated = HeldItem.BoundsAt(Vector3.zero, rotation);
        Vector3 position = surface.point - new Vector3(rotated.center.x, rotated.min.y, rotated.center.z) + Vector3.up * .01f;
        placementCollider=surface.collider;placementSurface=surface.collider.transform;
        placeLocalPosition=placementSurface.InverseTransformPoint(position);
        placeLocalRotation=Quaternion.Inverse(placementSurface.rotation)*rotation;
        placeLocalNormal=placementSurface.InverseTransformDirection(surface.normal);
        placeStart=HeldItem.transform.position;placeStartRotation=HeldItem.transform.rotation;
        placeElapsed=0;placing=true;
        return true;
    }

    private void AdvancePlacement(float deltaTime)
    {
        if(!placing)return;
        if(HeldItem==null || placementCollider==null || !placementCollider.enabled || !placementCollider.gameObject.activeInHierarchy ||
            placementSurface.TransformDirection(placeLocalNormal).y<.25f)
        {
            placing=false;placementCollider=null;placementSurface=null;
            if(HeldItem!=null){pickupStart=HeldItem.transform.position;pickupStartRotation=HeldItem.transform.rotation;pickupElapsed=0;}
            return;
        }
        placeElapsed+=Mathf.Max(0,deltaTime);
        float t=Mathf.Clamp01(placeElapsed/ManipulationMotion.PlaceSeconds),ease=ManipulationMotion.Ease(t);
        Vector3 position=placementSurface.TransformPoint(placeLocalPosition);
        Quaternion rotation=placementSurface.rotation*placeLocalRotation;
        HeldItem.SetCarriedPose(Vector3.Lerp(placeStart,position,ease)+Vector3.up*ManipulationMotion.Lift(t,.04f),
            Quaternion.Slerp(placeStartRotation,rotation,ease));
        if(t<1)return;
        releasePosition=HandPosition;releaseRotation=HandRotation;releaseElapsed=0;
        PickupItem placed=HeldItem;Transform surface=placementSurface;
        placing=false;placementCollider=null;placementSurface=null;HeldItem=null;
        placed.PlaceOnSurface(position,rotation,surface);
        ShowMessage("Abgelegt.");Learning.Placed(placed);
    }

    // Naming uses the nearest visible hit, never an object's open/pickup action.
    public bool TryIdentifyAtScreen(Vector2 screenPosition)
    {
        if (placing || playerCamera == null || experience == null || experience.MenuOpen ||
            experience.ModalOpen || experience.MapExpanded || Time.timeScale <= 0) return false;
        Camera camera = playerCamera.GetComponent<Camera>();
        if (camera == null) return false;
        Physics.SyncTransforms();
        if (!RaycastWorld(camera.ScreenPointToRay(screenPosition),interactionDistance,out RaycastHit hit)) return false;
        var part=hit.collider.GetComponentInParent<VocabularyPart>();
        var item=part != null && part.Owner != null ? part.Owner : hit.collider.GetComponentInParent<KitchenInteractable>();
        if (item == null || Practice != null && !Practice.AllowsIdentification(item)) return false;
        Identify(item,true);
        return true;
    }

    public void IdentifyFocused()
    {
        if (IdentificationTarget != null) Identify(IdentificationTarget);
        else if (focusedPart != null) IdentifyPart(focusedPart);
    }

    public void Identify(KitchenInteractable item) => Identify(item,false);
    private void Identify(KitchenInteractable item, bool fromPointer)
    {
        if (placing || item == null) return;
        if (Practice != null && !Practice.AllowsIdentification(item)) return;
        identifiedName = item.DisplayName; nameUntil = Time.unscaledTime + 3.5f;
        identifiedByPointer = fromPointer;
        if (Practice != null) Practice.Named(item,fromPointer);
        if (!Learning.Identified(item)) Voice.SpeakIdentification(item.DisplayName);
    }

    public void IdentifyFocusedPart()
    {
        if (CanIdentifyPart) IdentifyPart(focusedPart);
    }

    public void IdentifyPart(VocabularyPart part)
    {
        if (placing || Practice != null) return;
        if (part == null) return;
        identifiedByPointer = false;
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
        int count = Physics.RaycastNonAlloc(ray, worldHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // A full buffer may omit the nearest valid hit: preserve exact behavior in crowded areas.
        RaycastHit[] hits = count == worldHits.Length
            ? Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) : worldHits;
        if (hits != worldHits) count = hits.Length;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
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
        placing=false;placementCollider=null;placementSurface=null;releaseElapsed=1;
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
