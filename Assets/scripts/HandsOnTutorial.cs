using System;
using System.Collections.Generic;
using UnityEngine;

// Stage-zero rehearsal: real controls and physics, with lesson callbacks/saves suspended.
[DefaultExecutionOrder(300)]
public sealed class HandsOnTutorial : MonoBehaviour
{
    public enum PracticeStep { Move, Table, Aim, Name, Pickup, Rotate, Place, Complete }
    public bool Active { get; private set; }
    public PracticeStep Step { get; private set; }
    public bool Complete => Step == PracticeStep.Complete;
    public PickupItem Target { get; private set; }
    public Vector3 Destination { get; private set; }
    public int StepCount => 7;
    public IntroLessonCatalog.ControlsPracticeStep Instruction => Complete ? null : player.Learning.ControlsPracticeSteps[(int)Step];
    public string GermanName { get; private set; } = "";
    public string RouteStatus { get; private set; } = "";
    private PlayerInteraction player;
    private PlayerControls controls;
    private FirstPersonMovement movement;
    private FirstPersonMovement.PracticeView originalView;
    private Collider table, floor;
    private readonly Dictionary<PickupItem, LessonPlacementScenario.OriginalPose> originals = new Dictionary<PickupItem, LessonPlacementScenario.OriginalPose>();
    private readonly Dictionary<PickupItem, Vector3> originalScales = new Dictionary<PickupItem, Vector3>();
    private GameObject markers;
    private Material material;
    private LineRenderer pathLine, destinationRing, spoonRing;
    private readonly Collider[] overlaps = new Collider[64];
    private bool[] walkable;
    private int columns, rows, goal, lastStart = -1;
    private const float Cell = .2f;
    private Bounds floorBounds;
    private int[] previous;
    private readonly Queue<int> pending = new Queue<int>();
    private readonly List<int> route = new List<int>();
    private Vector3 previousPosition;
    private Vector3 placementPoint, lastRingCentre;
    private float walked, nextRouteTime;
    private bool named;
    private uint carryAtStart, rotateAtStart, placeAtStart;
    private PickupItem.PracticeInteractionState originalInteractions;
    private PickupItem originalHeld;
    private PickupItem.PracticeInteractionState originalHeldInteractions;
    private float originalCarryYaw;

    public bool Begin(PlayerInteraction owner)
    {
        if (Active) return true;
        var steps = owner.Learning.ControlsPracticeSteps;
        if (steps == null || steps.Length != StepCount) return false;
        player = owner; movement = owner.GetComponent<FirstPersonMovement>(); controls = owner.GetComponent<PlayerControls>();
        var pickups = FindObjectsByType<PickupItem>(FindObjectsInactive.Include);
        Target = Array.Find(pickups, item => item.name == "Loeffel_Spoon");
        table = GameObject.Find("TableTop")?.GetComponent<Collider>();
        floor = GameObject.Find("Floor")?.GetComponent<Collider>();
        if (Target == null || table == null || floor == null || movement == null) return false;
        originalHeld = player.HeldItem; originalCarryYaw = player.PracticeCarryYaw;
        if (originalHeld != null) originalHeldInteractions = originalHeld.CapturePracticeInteractions();
        player.CancelCarry();
        originalInteractions = Target.CapturePracticeInteractions();
        rotateAtStart = player.RotationSequence;
        originalView = movement.CapturePracticeView();
        player.Learning.BeginControlsPractice();
        Active = true;
        try
        {
            foreach (var item in pickups)
            {
                originals.Add(item, new LessonPlacementScenario.OriginalPose { parent = item.transform.parent,
                    position = item.transform.position, rotation = item.transform.rotation,
                    localPosition = item.transform.localPosition, localRotation = item.transform.localRotation,
                    active = item.gameObject.activeSelf });
                originalScales.Add(item, item.transform.localScale);
            }
            Bounds top = table.bounds;
            // Clear all portable tabletop occupants, including objects resting just above its collider.
            foreach (var item in originals.Keys)
            {
                Bounds b = item.BoundsAt(item.transform.position, item.transform.rotation);
                if (item != Target && b.min.y < top.max.y + .5f && b.max.y > top.max.y - .1f &&
                    b.min.x < top.max.x && b.max.x > top.min.x && b.min.z < top.max.z && b.max.z > top.min.z)
                    item.gameObject.SetActive(false);
            }
            Target.gameObject.SetActive(true); Target.transform.SetParent(null, true);
            Quaternion rotation = Target.PlacementRotation(0);
            Bounds bounds = Target.BoundsAt(Vector3.zero, rotation);
            Vector3 at = new Vector3(top.max.x - .22f, top.max.y + .01f, top.center.z);
            Target.transform.SetPositionAndRotation(at - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), rotation);
            movement.StartPracticeView(); Physics.SyncTransforms();
            BuildGrid();
            // Prefer an unobstructed side of the table, never a straight line through the chairs.
            int start = Nearest(movement.transform.position);
            goal = -1;
            foreach (var point in new[] {
                new Vector3(top.max.x + .65f, 0, top.center.z), new Vector3(top.min.x - .65f, 0, top.center.z),
                new Vector3(top.center.x, 0, top.min.z - .75f), new Vector3(top.center.x, 0, top.max.z + .75f) })
            {
                int candidate = Nearest(point);
                if (candidate >= 0 && Horizontal(CellWorld(candidate), point) < .35f &&
                    TutorialRouteSearch.Find(walkable, columns, start, candidate, previous, pending, route))
                { goal = candidate; break; }
            }
            if (goal < 0) throw new InvalidOperationException("No clear walking route to the tutorial table.");
            Destination = CellWorld(goal);
            // Put the spoon near whichever side was reachable, within normal interaction range.
            Vector3 near = new Vector3(Mathf.Clamp(Destination.x, top.min.x + .22f, top.max.x - .22f),
                top.max.y + .01f, Mathf.Clamp(Destination.z, top.min.z + .22f, top.max.z - .22f));
            placementPoint = near + Vector3.up * .015f;
            Target.transform.position = near - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            lastStart = -1; BuildMarkers();
            Step = PracticeStep.Move; walked = 0; named = false; GermanName = "";
            previousPosition = player.transform.position; lastStart = -1; nextRouteTime = 0;
            carryAtStart = Target.CarrySequence; rotateAtStart = player.RotationSequence; placeAtStart = Target.PlacementSequence;
            player.Voice.Stop();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("Controls practice could not start: " + error.Message);
            Finish(); return false;
        }
    }

    public bool AllowsPickup(KitchenInteractable item) => Active && Step == PracticeStep.Pickup && item == Target;
    public bool AllowsIdentification(KitchenInteractable item) => Active && Step == PracticeStep.Name && item == Target;
    public bool AllowsRotation => Active && Step == PracticeStep.Rotate;
    public bool AllowsPlacement(Transform surface) => Active && Step == PracticeStep.Place && surface == table.transform;
    public void Named(KitchenInteractable item)
    {
        if (!AllowsIdentification(item)) return;
        named = true; GermanName = item.DisplayName;
    }

    private void Update()
    {
        if (!Active || Complete) return;
        if (!controls.GameplayActive || Time.timeScale <= 0) { previousPosition = player.transform.position; return; }
        // Loss of focus and application suspension must not satisfy movement by themselves.
        walked += Horizontal(previousPosition, player.transform.position);
        previousPosition = player.transform.position;
        if (HandsOnTutorialRules.Ready((int)Step, walked, Horizontal(player.transform.position, Destination),
            player.FocusedObject == Target, named,
            player.HeldItem == Target && Target.CarrySequence != carryAtStart,
            player.RotationSequence != rotateAtStart,
            player.HeldItem == null && Target.PlacementSequence != placeAtStart && Target.LastPlacementSurface == table.transform))
        {
            Step++;
            controls.ResetInput();
        }
        bool walking = Step <= PracticeStep.Table;
        pathLine.enabled = destinationRing.enabled = walking;
        spoonRing.enabled = !walking && !Complete && (player.HeldItem == null || Step == PracticeStep.Place);
        if (walking && Time.unscaledTime >= nextRouteTime)
        { nextRouteTime = Time.unscaledTime + .2f; UpdateRoute(); }
        if (spoonRing.enabled)
        {
            Vector3 centre = Step == PracticeStep.Place ? placementPoint : Target.transform.position + Vector3.up * .025f;
            if (spoonRing.positionCount == 0 || !lastRingCentre.Equals(centre))
            { Ring(spoonRing, centre, .16f); lastRingCentre = centre; }
        }
    }

    private static float Horizontal(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    private Vector3 CellWorld(int index) => new Vector3(floorBounds.min.x + (index % columns + .5f) * Cell,
        floorBounds.max.y + .025f, floorBounds.min.z + (index / columns + .5f) * Cell);
    private int Nearest(Vector3 point)
    {
        int nearest = -1; float distance = float.PositiveInfinity;
        for (int i = 0; i < walkable.Length; i++)
        {
            if (!walkable[i]) continue;
            Vector3 delta = CellWorld(i) - point;
            float d = delta.x * delta.x + delta.z * delta.z;
            if (d < distance) { distance = d; nearest = i; }
        }
        return nearest;
    }
    private void BuildGrid()
    {
        floorBounds = floor.bounds;
        columns = Mathf.FloorToInt(floorBounds.size.x / Cell); rows = Mathf.FloorToInt(floorBounds.size.z / Cell);
        walkable = new bool[columns * rows]; previous = new int[walkable.Length];
        var controller = player.GetComponent<CharacterController>();
        float radius = controller.radius + .06f, height = controller.height;
        for (int i = 0; i < walkable.Length; i++)
        {
            Vector3 p = CellWorld(i);
            if (p.x - radius < floorBounds.min.x || p.x + radius > floorBounds.max.x ||
                p.z - radius < floorBounds.min.z || p.z + radius > floorBounds.max.z) continue;
            int count = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * radius,
                p + Vector3.up * Mathf.Max(radius, height - radius), radius, overlaps,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            bool clear = count < overlaps.Length;
            for (int n = 0; n < count && clear; n++)
                if (!overlaps[n].transform.IsChildOf(player.transform)) clear = false;
            walkable[i] = clear;
        }
    }
    private void BuildMarkers()
    {
        markers = new GameObject("Controls practice markers") { hideFlags = HideFlags.DontSave };
        markers.transform.SetParent(transform, false);
        material = new Material(Resources.Load<Material>("Lessons/PlacementZone")) { hideFlags = HideFlags.DontSave };
        material.SetColor("_BaseColor", new Color(.25f, 1, .65f));
        pathLine = Line("Walking route", .055f); destinationRing = Line("Stand here", .04f); spoonRing = Line("Spoon highlight", .018f);
        Ring(destinationRing, Destination, .35f); UpdateRoute();
        spoonRing.enabled = false;
    }
    private LineRenderer Line(string label, float width)
    {
        var child = new GameObject(label); child.transform.SetParent(markers.transform, false);
        var line = child.AddComponent<LineRenderer>(); line.sharedMaterial = material;
        line.positionCount = 0;
        line.useWorldSpace = true; line.startWidth = line.endWidth = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
        line.alignment = LineAlignment.View; line.numCornerVertices = 2;
        return line;
    }
    private static void Ring(LineRenderer line, Vector3 centre, float radius)
    {
        line.positionCount = 33;
        for (int i = 0; i <= 32; i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            line.SetPosition(i, centre + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }
    }
    private void UpdateRoute()
    {
        int start = Nearest(player.transform.position);
        if (start == lastStart) return;
        lastStart = start;
        bool found = TutorialRouteSearch.Find(walkable, columns, start, goal, previous, pending, route);
        RouteStatus = found ? "Follow the green line to the ring." : "Return to the green route, or Skip to your lesson.";
        pathLine.positionCount = found ? route.Count : 0;
        for (int i = 0; found && i < route.Count; i++) pathLine.SetPosition(i, CellWorld(route[i]));
    }

    public void Finish()
    {
        if (!Active) return;
        // Restore while callbacks/saving are still suspended, including a mid-carry Skip.
        if (player != null) { player.CancelCarry(); player.Voice.Stop(); }
        foreach (var pair in originals)
        {
            if (pair.Key == null) continue;
            var t = pair.Key.transform; var pose = pair.Value;
            t.SetParent(pose.parent, false);
            if (pose.parent != null) { t.localPosition = pose.localPosition; t.localRotation = pose.localRotation; }
            else t.SetPositionAndRotation(pose.position, pose.rotation);
            t.localScale = originalScales[pair.Key];
            pair.Key.gameObject.SetActive(pose.active);
        }
        originals.Clear(); originalScales.Clear();
        if (Target != null) Target.RestorePracticeInteractions(originalInteractions);
        if (player != null) player.RestorePracticeRotation(rotateAtStart);
        if (movement != null) movement.RestorePracticeView(originalView);
        if (player != null && originalHeld != null) player.ResumePracticeCarry(originalHeld, originalCarryYaw, originalHeldInteractions);
        originalHeld = null;
        if (markers != null) { markers.SetActive(false); Destroy(markers); }
        if (material != null) Destroy(material);
        markers = null; material = null; pathLine = destinationRing = spoonRing = null;
        walkable = null; previous = null; route.Clear(); pending.Clear(); Array.Clear(overlaps, 0, overlaps.Length);
        if (player != null) { player.ClearPracticeFeedback(); if (player.Learning != null) player.Learning.EndControlsPractice(); }
        Active = false;
    }
    private void OnDestroy() => Finish();
}

public static class HandsOnTutorialRules
{
    public static bool Ready(int step, float walked, float distance, bool aimed, bool named, bool picked, bool rotated, bool placed)
    {
        switch (step)
        {
            case 0: return walked >= .65f;
            case 1: return distance <= .4f;
            case 2: return aimed;
            case 3: return named;
            case 4: return picked;
            case 5: return rotated;
            case 6: return placed;
            default: return false;
        }
    }
}

// Reusable cardinal BFS: no diagonal corner cutting and no per-route arrays.
public static class TutorialRouteSearch
{
    public static bool Find(bool[] cells, int columns, int start, int goal, int[] previous, Queue<int> pending, List<int> route)
    {
        route.Clear(); pending.Clear();
        if (columns <= 0 || start < 0 || goal < 0 || start >= cells.Length || goal >= cells.Length || !cells[start] || !cells[goal]) return false;
        for (int i = 0; i < previous.Length; i++) previous[i] = -1;
        previous[start] = start; pending.Enqueue(start);
        while (pending.Count > 0 && previous[goal] < 0)
        {
            int at = pending.Dequeue(), x = at % columns;
            if (x > 0) Visit(at - 1, at);
            if (x < columns - 1) Visit(at + 1, at);
            Visit(at - columns, at); Visit(at + columns, at);
        }
        if (previous[goal] < 0) return false;
        for (int at = goal; at != start; at = previous[at]) route.Add(at);
        route.Add(start); route.Reverse(); return true;
        void Visit(int cell, int from)
        {
            if (cell < 0 || cell >= cells.Length || !cells[cell] || previous[cell] >= 0) return;
            previous[cell] = from; pending.Enqueue(cell);
        }
    }
}
