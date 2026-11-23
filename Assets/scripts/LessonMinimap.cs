using UnityEngine;
using System.Collections.Generic;

// Flat floor-plan rendering avoids an extra camera/render texture on mobile.
public sealed class LessonMinimap : MonoBehaviour
{
    public enum TargetKind { Find, Remove, Table, PutAway }
    private PlayerInteraction player;
    private Bounds[] floors, walls;
    private Openable[] doors;
    public Rect WorldArea { get; private set; }
    private Texture2D circle, arrow;
    private GUIStyle label;
    private static readonly Color Ink = new Color(.035f, .075f, .10f, .97f);
    private static readonly Color Mint = new Color(.3f, 1f, .8f);
    private static readonly Color Gold = new Color(1f, .77f, .25f);

    private void Start()
    {
        player = GetComponent<PlayerInteraction>();
        var floorList = new System.Collections.Generic.List<Bounds>();
        var wallList = new System.Collections.Generic.List<Bounds>();
        floorList.Add(GameObject.Find("Floor").GetComponent<Collider>().bounds);
        Transform home = GameObject.Find("HomeExpansion")?.transform;
        if (home != null)
            foreach (BoxCollider part in home.GetComponentsInChildren<BoxCollider>())
            {
                if (part.name.EndsWith("_Floor")) floorList.Add(part.bounds);
                else if (part.bounds.min.y < 1.2f && part.bounds.max.y > 1.2f && part.GetComponentInParent<Openable>() == null) wallList.Add(part.bounds);
            }
        foreach (BoxCollider part in FindObjectsByType<BoxCollider>())
            if (part.name.StartsWith("WindowWall_") || part.name.StartsWith("DoorWall_") || part.name.StartsWith("Wall_")) wallList.Add(part.bounds);
        floors = floorList.ToArray(); walls = wallList.ToArray();
        doors = FindObjectsByType<Openable>();
        Bounds total = floors[0];
        foreach (Bounds floor in floors) total.Encapsulate(floor);
        WorldArea = new Rect(total.min.x - .3f, total.min.z - .3f, total.size.x + .6f, total.size.z + .6f);
        circle = Shape(false); arrow = Shape(true);
    }

    public static Rect Layout(float width) => new Rect(18, 18, 120, 108);
    public static Vector2 Project(Vector3 world, Rect worldArea, Rect map)
    {
        return new Vector2(map.x + (world.x - worldArea.xMin) / worldArea.width * map.width,
            map.yMax - (world.z - worldArea.yMin) / worldArea.height * map.height);
    }
    private Rect BoundsRect(Bounds bounds, Rect map)
    {
        Vector2 topLeft = Project(new Vector3(bounds.min.x, 0, bounds.max.z), WorldArea, map);
        Vector2 bottomRight = Project(new Vector3(bounds.max.x, 0, bounds.min.z), WorldArea, map);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, Mathf.Max(topLeft.x + 1, bottomRight.x), Mathf.Max(topLeft.y + 1, bottomRight.y));
    }
    private static void Fill(Rect rect, Color color, Texture texture = null)
    {
        Color before = GUI.color; GUI.color = color;
        GUI.DrawTexture(rect, texture != null ? texture : Texture2D.whiteTexture); GUI.color = before;
    }
    public void Draw(Rect panel)
    {
        if (Event.current.type != EventType.Repaint || player == null || floors == null || circle == null) return;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = Color.white;
        }
        Fill(new Rect(panel.x - 2, panel.y - 2, panel.width + 4, panel.height + 4), new Color(.2f, .39f, .42f, .95f));
        Fill(panel, Ink);
        Rect available = new Rect(panel.x + 8, panel.y + 16, panel.width - 16, panel.height - 38);
        float scale = Mathf.Min(available.width / WorldArea.width, available.height / WorldArea.height);
        Rect map = new Rect(available.center.x - WorldArea.width * scale / 2, available.center.y - WorldArea.height * scale / 2, WorldArea.width * scale, WorldArea.height * scale);
        for (int i = 0; i < floors.Length; i++) Fill(BoundsRect(floors[i], map), i == 0 ? new Color(.12f, .33f, .34f) : new Color(.13f, .22f, .27f));
        foreach (Bounds wall in walls) Fill(BoundsRect(wall, map), new Color(.63f, .76f, .76f));
        foreach (Openable door in doors)
        {
            if (door == null || (door.transform.parent != null && door.transform.parent.name != "HomeExpansion") && door.name != "Hinge_die_Tuer") continue;
            Collider leaf = door.GetComponentInChildren<Collider>();
            if (leaf != null) Fill(BoundsRect(leaf.bounds, map), new Color(.4f, .6f, .61f));
        }
        Collider table = player.Learning.MapTable;
        if (table != null) Fill(BoundsRect(table.bounds, map), new Color(.33f, .47f, .43f));
        Vector2 at = Clamp(Project(player.transform.position, WorldArea, map), available, 7);
        bool hasTarget = player.Learning.TryGetMapTarget(out Vector3 target, out TargetKind kind);
        string footer = "";
        if (hasTarget)
        {
            Vector2 point = Clamp(Project(target, WorldArea, map), available, 8);
            DrawRoute(player.transform.position, target, map);
            float pulse = 7 + Mathf.Sin(Time.unscaledTime * 3) * 1.5f;
            Fill(new Rect(point.x - pulse, point.y - pulse, pulse * 2, pulse * 2), new Color(1, .77f, .25f, .28f), circle);
            Matrix4x4 before = GUI.matrix; GUIUtility.RotateAroundPivot(45, point);
            Fill(new Rect(point.x - 3, point.y - 3, 6, 6), Gold); GUI.matrix = before;
            float distance = Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(target.x, target.z));
            footer = (kind == TargetKind.Remove ? "Entfernen" : kind == TargetKind.Table ? "Tisch" : kind == TargetKind.PutAway ? "Ablegen" : "Ziel") + " · " + distance.ToString("0.0") + " m";
        }
        Fill(new Rect(at.x - 8, at.y - 8, 16, 16), Ink, circle);
        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(player.transform.eulerAngles.y, at);
        Fill(new Rect(at.x - 6, at.y - 7, 12, 14), Mint, arrow); GUI.matrix = matrix;
        GUI.Label(new Rect(panel.x, panel.y - 1, panel.width, 17), panel.width > 200 ? "N · Tippen zum Schließen" : "N · Vergrößern", label);
        GUI.Label(new Rect(panel.x + 3, panel.yMax - 21, panel.width - 6, 20), footer, label);
    }
    // Floor-connected walking guidance; closed doors remain traversable because they can be opened.
    private const float RouteCell = .25f;
    private bool[] walkable;
    private int columns, rows;
    private int[] previous;
    private Queue<int> pending;
    private int cachedStart = -1, cachedGoal = -1;
    private Vector3 nearestStartPosition, nearestGoalPosition;
    private int nearestStart = -1, nearestGoal = -1;
    private bool hasNearest;
    private readonly List<Vector3> route = new List<Vector3>();

    private Vector3 CellWorld(int index) => new Vector3(WorldArea.xMin + (index % columns + .5f) * RouteCell,
        1.2f, WorldArea.yMin + (index / columns + .5f) * RouteCell);

    private void BuildRouteGrid()
    {
        columns = Mathf.CeilToInt(WorldArea.width / RouteCell);
        rows = Mathf.CeilToInt(WorldArea.height / RouteCell);
        walkable = new bool[columns * rows];
        previous = new int[walkable.Length];
        pending = new Queue<int>(walkable.Length);
        for (int i = 0; i < walkable.Length; i++)
        {
            Vector3 point = CellWorld(i);
            foreach (Bounds floor in floors)
                if (point.x >= floor.min.x && point.x <= floor.max.x && point.z >= floor.min.z && point.z <= floor.max.z)
                { walkable[i] = true; break; }
            if (!walkable[i]) continue;
            foreach (Bounds wall in walls)
            {
                Bounds clearance = wall;
                clearance.Expand(new Vector3(.3f, 0, .3f));
                if (clearance.Contains(point)) { walkable[i] = false; break; }
            }
        }
    }

    private int NearestCell(Vector3 world)
    {
        int best = -1;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < walkable.Length; i++)
        {
            if (!walkable[i]) continue;
            Vector3 delta = CellWorld(i) - world;
            float squared = delta.x * delta.x + delta.z * delta.z;
            if (squared < distance) { distance = squared; best = i; }
        }
        return best;
    }

    private void DrawRoute(Vector3 start, Vector3 goal, Rect map)
    {
        if (walkable == null) BuildRouteGrid();
        // The grid is static; exact unchanged positions have the same nearest cell.
        if (!hasNearest || !start.Equals(nearestStartPosition))
        { nearestStart = NearestCell(start); nearestStartPosition = start; }
        if (!hasNearest || !goal.Equals(nearestGoalPosition))
        { nearestGoal = NearestCell(goal); nearestGoalPosition = goal; }
        hasNearest = true;
        int first = nearestStart, last = nearestGoal;
        if (first != cachedStart || last != cachedGoal)
        {
            cachedStart = first; cachedGoal = last; route.Clear();
            if (first < 0 || last < 0) return;
            for (int i = 0; i < previous.Length; i++) previous[i] = -1;
            pending.Clear();
            previous[first] = first; pending.Enqueue(first);
            while (pending.Count > 0 && previous[last] < 0)
            {
                int at = pending.Dequeue();
                int x = at % columns, y = at / columns;
                Visit(x - 1, y, at); Visit(x + 1, y, at); Visit(x, y - 1, at); Visit(x, y + 1, at);
            }
            if (previous[last] >= 0)
            {
                for (int at = last; at != first; at = previous[at]) route.Add(CellWorld(at));
                route.Add(CellWorld(first));
            }
            void Visit(int x, int y, int from)
            {
                if (x < 0 || x >= columns || y < 0 || y >= rows) return;
                int index = y * columns + x;
                if (!walkable[index] || previous[index] >= 0) return;
                previous[index] = from; pending.Enqueue(index);
            }
        }
        for (int i = 1; i < route.Count; i++)
        {
            Vector2 a = Project(route[i - 1], WorldArea, map), b = Project(route[i], WorldArea, map);
            Matrix4x4 before = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
            Fill(new Rect(a.x, a.y - 1, Vector2.Distance(a, b) + 1, 2), Gold);
            GUI.matrix = before;
        }
    }
    private static Vector2 Clamp(Vector2 point, Rect rect, float padding) => new Vector2(Mathf.Clamp(point.x, rect.xMin + padding, rect.xMax - padding), Mathf.Clamp(point.y, rect.yMin + padding, rect.yMax - padding));
    private static Texture2D Shape(bool triangle)
    {
        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            bool filled = triangle ? Mathf.Abs(x - 15.5f) <= (31 - y) * .44f : Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) < 15;
            texture.SetPixel(x, y, new Color(1, 1, 1, filled ? 1 : 0));
        }
        texture.Apply(false, true); return texture;
    }
    private void OnDestroy() { if (circle != null) Destroy(circle); if (arrow != null) Destroy(arrow); }
}
