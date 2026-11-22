using System;
using System.Collections.Generic;
using UnityEngine;

// Pure geometry gate shared with managed tests; the whole footprint must fit, not just its centre.
public static class LessonPlacementRules
{
    public static bool Accepts(float minX, float maxX, float minZ, float maxZ, float bottom,
        float zoneMinX, float zoneMaxX, float zoneMinZ, float zoneMaxZ, float surfaceY)
    {
        return minX <= maxX && minZ <= maxZ && minX >= zoneMinX - .01f && maxX <= zoneMaxX + .01f &&
            minZ >= zoneMinZ - .01f && maxZ <= zoneMaxZ + .01f && Math.Abs(bottom - surfaceY) <= .035f;
    }
}

// Temporary practice layout. It uses the existing table/items and never persists its poses.
public sealed class LessonPlacementScenario : MonoBehaviour
{
    public sealed class OriginalPose
    {
        public Transform parent;
        public Vector3 position, localPosition;
        public Quaternion rotation, localRotation;
        public bool active;
    }
    private readonly Dictionary<PickupItem, OriginalPose> originals = new Dictionary<PickupItem, OriginalPose>();
    private GameObject markers;
    private Material markerMaterial;
    private Collider table;
    private PickupItem target, reference;
    private Vector3 referencePosition;
    private Quaternion referenceRotation;
    private Bounds zone;
    public bool Active => originals.Count > 0;
    public PickupItem Reference => reference;
    public Vector3 ZonePosition => zone.center;
    public Bounds ZoneBounds => zone;

    public bool TryGetOriginal(PickupItem item, out OriginalPose pose) => originals.TryGetValue(item, out pose);

    public void Prepare(IntroLessonCatalog.Task task, PickupItem[] items, Collider surface, PlayerInteraction player)
    {
        player.CancelCarry();
        if (!Active)
            foreach (PickupItem item in items)
                originals.Add(item, new OriginalPose { parent = item.transform.parent, position = item.transform.position,
                    rotation = item.transform.rotation, localPosition = item.transform.localPosition,
                    localRotation = item.transform.localRotation, active = item.gameObject.activeSelf });
        RestorePoses();
        ClearMarkers();
        table = surface;
        if (table == null) throw new InvalidOperationException("Placement lessons require TableTop.");
        target = Array.Find(items, item => item.name == task.targetName);
        reference = Array.Find(items, item => item.name == task.referenceName);
        if (target == null || reference == null || target == reference) throw new InvalidOperationException("Invalid placement pair.");
        Bounds top = table.bounds;
        // The existing table is axis-aligned. A visible arrow makes front/back independent of the camera.
        Vector3 centre = new Vector3(top.center.x, top.max.y, top.center.z);
        Vector3 offset = task.relation == "vor" ? Vector3.back * .36f :
            task.relation == "hinter" ? Vector3.forward * .36f : Vector3.right * .46f;
        zone = new Bounds(centre + offset, new Vector3(.43f, .01f, .36f));
        foreach (PickupItem item in items)
        {
            Bounds bounds = item.BoundsAt(item.transform.position, item.transform.rotation);
            if (item != target && item != reference && bounds.min.x < top.max.x && bounds.max.x > top.min.x &&
                bounds.min.z < top.max.z && bounds.max.z > top.min.z && bounds.min.y < top.max.y + .4f && bounds.max.y > top.max.y - .1f)
                item.gameObject.SetActive(false);
        }
        Put(reference, centre);
        Put(target, centre + Vector3.left * .68f);
        referencePosition = reference.transform.position;
        referenceRotation = reference.transform.rotation;
        markers = new GameObject("LessonPlacementMarkers") { hideFlags = HideFlags.DontSave };
        Material template = Resources.Load<Material>("Lessons/PlacementZone");
        if (template == null) throw new InvalidOperationException("Missing placement marker material.");
        markerMaterial = new Material(template) { hideFlags = HideFlags.DontSave };
        float y = top.max.y + .007f;
        Vector3 z = zone.center;
        Line("TargetZone", new[] { new Vector3(z.x-.215f,y,z.z-.18f),new Vector3(z.x+.215f,y,z.z-.18f),
            new Vector3(z.x+.215f,y,z.z+.18f),new Vector3(z.x-.215f,y,z.z+.18f),new Vector3(z.x-.215f,y,z.z-.18f) });
        float arrowX = top.max.x - .12f;
        Line("ForwardArrow", new[] { new Vector3(arrowX,y,top.center.z+.18f), new Vector3(arrowX,y,top.center.z-.18f),
            new Vector3(arrowX-.07f,y,top.center.z-.08f),new Vector3(arrowX,y,top.center.z-.18f),new Vector3(arrowX+.07f,y,top.center.z-.08f) });
        Physics.SyncTransforms();
    }

    private void Put(PickupItem item, Vector3 at)
    {
        item.gameObject.SetActive(true);
        item.transform.SetParent(null, true);
        Quaternion rotation = item.PlacementRotation(0);
        Bounds bounds = item.BoundsAt(Vector3.zero, rotation);
        item.transform.SetPositionAndRotation(at - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) + Vector3.up * .01f, rotation);
    }

    public bool ContainsTarget()
    {
        if (!Active || target == null || reference == null || target.IsHeld || reference.IsHeld ||
            target.LastPlacementSurface != table.transform ||
            (reference.transform.position - referencePosition).sqrMagnitude > .0001f ||
            Quaternion.Angle(reference.transform.rotation, referenceRotation) > 1f) return false;
        Bounds b = target.BoundsAt(target.transform.position, target.transform.rotation);
        return LessonPlacementRules.Accepts(b.min.x,b.max.x,b.min.z,b.max.z,b.min.y,
            zone.min.x,zone.max.x,zone.min.z,zone.max.z,table.bounds.max.y);
    }

    private void Line(string name, Vector3[] points)
    {
        var go = new GameObject(name); go.transform.SetParent(markers.transform, false);
        var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = markerMaterial;
        line.useWorldSpace = true; line.positionCount = points.Length; line.SetPositions(points);
        line.startWidth = line.endWidth = .012f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
    }

    private void RestorePoses()
    {
        foreach (var pair in originals)
        {
            if (pair.Key == null) continue;
            pair.Key.transform.SetParent(pair.Value.parent, false);
            if (pair.Value.parent != null)
            { pair.Key.transform.localPosition = pair.Value.localPosition; pair.Key.transform.localRotation = pair.Value.localRotation; }
            else pair.Key.transform.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
            pair.Key.gameObject.SetActive(pair.Value.active);
        }
    }
    private void ClearMarkers()
    {
        if (markers != null) { markers.SetActive(false); Destroy(markers); }
        if (markerMaterial != null) Destroy(markerMaterial);
    }
    public void Finish(PlayerInteraction player)
    {
        if (!Active) return;
        player.CancelCarry(); RestorePoses(); originals.Clear(); ClearMarkers();
        target = reference = null; Physics.SyncTransforms();
    }
    private void OnDestroy() { RestorePoses(); ClearMarkers(); }
}
