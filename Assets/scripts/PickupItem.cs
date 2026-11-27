using UnityEngine;

public sealed class PickupItem : KitchenInteractable
{
    [SerializeField] private Vector3 placementEulerAngles;
    [SerializeField] private int lessonOrder;
    public int LessonOrder => lessonOrder;
    private Collider[] itemColliders;
    private bool[] colliderStates;
    private Rigidbody body;
    private Transform previousParent;
    private Vector3 previousPosition;
    private Quaternion previousRotation;

    public Bounds LocalBounds { get; private set; }
    public Vector3 LocalGripPoint { get; private set; }
    public bool IsHeld { get; private set; }
    public uint CarrySequence { get; private set; }

    public uint PlacementSequence { get; private set; }
    public uint LastPlacedCarrySequence { get; private set; }
    public Transform LastPlacementSurface { get; private set; }
    internal struct PracticeInteractionState
    {
        public uint carry, place, placedCarry;
        public Transform surface;
    }
    internal PracticeInteractionState CapturePracticeInteractions() => new PracticeInteractionState {
        carry = CarrySequence, place = PlacementSequence, placedCarry = LastPlacedCarrySequence, surface = LastPlacementSurface
    };
    internal void RestorePracticeInteractions(PracticeInteractionState state)
    {
        CarrySequence = state.carry; PlacementSequence = state.place;
        LastPlacedCarrySequence = state.placedCarry; LastPlacementSurface = state.surface;
    }

    private void Awake()
    {
        itemColliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[itemColliders.Length];
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        bool first = true;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (MeshFilter mesh in GetComponentsInChildren<MeshFilter>())
        {
            if (mesh.sharedMesh == null)
                continue;
            Bounds meshBounds = mesh.sharedMesh.bounds;
            Matrix4x4 matrix = transform.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(Corner(meshBounds, i));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                    bounds.Encapsulate(point);
            }
        }
        LocalBounds = bounds;
        // Default to the right/near edge; utensils must be held by their handle.
        LocalGripPoint = bounds.center + new Vector3(bounds.extents.x*.80f,
            -Mathf.Min(.018f,bounds.extents.y*.3f),-Mathf.Min(.05f,bounds.extents.z*.3f));
        foreach (var mesh in GetComponentsInChildren<MeshFilter>())
            if (mesh.sharedMesh != null && (mesh.name == "SpoonHandle" || mesh.name == "ForkHandle" || mesh.name == "KnifeHandle"))
            { LocalGripPoint = transform.InverseTransformPoint(mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)); break; }
    }

    public override string GetAction(PlayerInteraction player) => player.HeldItem == null ? "Aufheben" : "Zuerst ablegen";

    public override void Interact(PlayerInteraction player)
    {
        player.TryPickUp(this);
    }

    public void BeginCarry()
    {
        CarrySequence = unchecked(CarrySequence + 1);
        previousParent = transform.parent;
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        for (int i = 0; i < itemColliders.Length; i++)
        {
            colliderStates[i] = itemColliders[i].enabled;
            itemColliders[i].enabled = false;
        }
        transform.SetParent(null, true);
        IsHeld = true;
    }

    public void SetCarriedPose(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        body.position = position;
        body.rotation = rotation;
    }

    public Bounds BoundsAt(Vector3 position, Quaternion rotation)
    {
        Bounds result = new Bounds(position + rotation * LocalBounds.center, Vector3.zero);
        for (int i = 0; i < 8; i++)
            result.Encapsulate(position + rotation * Corner(LocalBounds, i));
        return result;
    }

    public Quaternion PlacementRotation(float yaw)
    {
        return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(placementEulerAngles);
    }

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(previousParent, true);
        SetCarriedPose(position, rotation);
        RestoreColliders();
    }

    public void PlaceOnSurface(Vector3 position, Quaternion rotation, Transform surface)
    {
        PlacementSequence = unchecked(PlacementSequence + 1);
        LastPlacedCarrySequence = CarrySequence;
        LastPlacementSurface = surface;
        Openable movingSurface = surface != null ? surface.GetComponentInParent<Openable>() : null;
        Transform parent = movingSurface != null ? movingSurface.transform :
            previousParent != null && previousParent.GetComponentInParent<Openable>() == null ? previousParent : null;
        transform.SetParent(parent, true);
        SetCarriedPose(position, rotation);
        RestoreColliders();
    }

    public void ReturnToPreviousPose()
    {
        if (!IsHeld)
            return;
        Place(previousPosition, previousRotation);
    }

    private void RestoreColliders()
    {
        for (int i = 0; i < itemColliders.Length; i++)
            itemColliders[i].enabled = colliderStates[i];
        IsHeld = false;
        Physics.SyncTransforms();
    }

    private static Vector3 Corner(Bounds bounds, int index)
    {
        return bounds.center + Vector3.Scale(bounds.extents, new Vector3(
            (index & 1) == 0 ? -1f : 1f,
            (index & 2) == 0 ? -1f : 1f,
            (index & 4) == 0 ? -1f : 1f));
    }
}
