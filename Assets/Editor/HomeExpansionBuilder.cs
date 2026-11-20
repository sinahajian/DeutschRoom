using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Empty architectural shell connected to the kitchen's existing south door.
public static class HomeExpansionBuilder
{
    public const int AddedDoors = 4;
    private static Transform root;
    private static Material wall, floor, ceiling, trim, door, tile, glass;
    [MenuItem("Tools/DeutschRoom/Rebuild Empty Home")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        GameObject previous = GameObject.Find("HomeExpansion");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Home")) AssetDatabase.CreateFolder("Assets/Materials", "Home");
        wall = Mat("Wall", new Color(.86f, .84f, .78f)); floor = Mat("Floor", new Color(.51f, .36f, .23f));
        ceiling = Mat("Ceiling", new Color(.94f, .94f, .9f)); trim = Mat("Trim", new Color(.94f, .91f, .81f));
        door = Mat("Door", new Color(.63f, .46f, .29f)); tile = Mat("Tile", new Color(.57f, .71f, .70f));
        glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WindowGlass.mat");
        root = new GameObject("HomeExpansion").transform;
        Room("LivingRoom", new Rect(-5, -10.5f, 6.8f, 5.5f), floor);
        Room("Bedroom1", new Rect(-5, -16, 6.8f, 5.5f), floor);
        Room("Bedroom2", new Rect(3.8f, -11, 5.7f, 6), floor);
        Room("BathroomWC", new Rect(3.8f, -16, 5.7f, 5), tile);
        Room("Hallway", new Rect(1.8f, -16, 2, 11), floor);
        // Outer envelope: the existing kitchen wall closes the north-west side.
        WallZ("NorthEast", -5, 5, 9.5f);
        WallZ("SouthWest", -16, -5, 1.8f);
        WallZ("SouthEast", -16, 3.8f, 9.5f);
        WallZ("HallEnd", -16, 1.8f, 3.8f);
        WallZ("WestDivider", -10.5f, -5, 1.8f);
        WallZ("EastDivider", -11, 3.8f, 9.5f);
        WindowWall("LivingWindow", -5, -10.5f, -5, -7.8f, false);
        WindowWall("Bedroom1Window", -5, -16, -10.5f, -13.3f, false);
        WindowWall("Bedroom2Window", 9.5f, -11, -5, -8, false);
        WindowWall("BathroomWindow", 9.5f, -16, -11, -13.5f, true);
        CorridorWall("West", 1.8f, -16, -5, new[] { -13.1f, -7.6f });
        CorridorWall("East", 3.8f, -16, -5, new[] { -13.4f, -8f });
        Door("Bedroom1Door", 1.8f, -13.1f, -1);
        Door("LivingRoomDoor", 1.8f, -7.6f, -1);
        Door("BathroomDoor", 3.8f, -13.4f, 1);
        Door("Bedroom2Door", 3.8f, -8f, 1);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        const string section = "مرحله ۱۲ — ساختمان خانه، بدون وسایل";
        string docs = File.ReadAllText("Documentation/tools.txt"); int start = docs.IndexOf(section, StringComparison.Ordinal);
        string laterSections = "";
        if (start >= 0)
        {
            int next = docs.IndexOf("\nمرحله ", start + section.Length, StringComparison.Ordinal);
            if (next >= 0) laterSections = "\n" + docs.Substring(next);
            docs = docs.Substring(0, start).TrimEnd();
        }
        File.WriteAllText("Documentation/tools.txt", docs.TrimEnd() + "\n\n" + section + "\nDeutsch | فارسی | English\n" +
            "das Wohnzimmer | پذیرایی | living room\ndas Schlafzimmer | اتاق خواب | bedroom\ndas Badezimmer | حمام | bathroom\ndie Toilette | دستشویی | toilet\nder Flur | راهرو | hallway\ndie Wand | دیوار | wall\nder Boden | کف | floor\ndie Decke | سقف | ceiling\ndie Zimmertür | در اتاق | room door\ndas Fenster | پنجره | window\n\nدو اتاق خواب خالی، یک پذیرایی خالی و یک حمام/دستشویی مشترک خالی از راهروی متصل به درِ قبلی آشپزخانه قابل دسترسی‌اند. تنها کف، دیوار، سقف، پنجره، چهار در و روشنایی محیطی ساخته شده‌اند؛ وسیله یا تجهیزات حمام اضافه نشده است.\n" + laterSections);
        Debug.Log("HOME_EXPANSION_BUILD_PASS: two empty bedrooms, living room, combined bathroom/WC, connected hallway and four doors.");
    }
    public static void BuildAndValidate() { Build(); InteractionValidation.Run(); }

    private static void Room(string name, Rect bounds, Material material)
    {
        Transform group = new GameObject(name).transform; group.SetParent(root, false);
        Part(group, name + "_Floor", new Vector3(bounds.center.x, -.1f, bounds.center.y), new Vector3(bounds.width, .2f, bounds.height), material);
        Part(group, name + "_Ceiling", new Vector3(bounds.center.x, 3.1f, bounds.center.y), new Vector3(bounds.width, .2f, bounds.height), ceiling);
        GameObject lamp = new GameObject("AmbientRoomLight", typeof(Light)); lamp.transform.SetParent(group, false);
        lamp.transform.position = new Vector3(bounds.center.x, 2.7f, bounds.center.y);
        Light light = lamp.GetComponent<Light>(); light.type = LightType.Point; light.range = 7.5f; light.intensity = 2.2f; light.color = new Color(1, .92f, .8f);
        light.shadows = LightShadows.None;
    }
    private static void WallZ(string name, float z, float left, float right)
    { Part(root, name, new Vector3((left + right) / 2, 1.5f, z), new Vector3(right - left, 3, .16f), wall); }
    private static void WallX(string name, float x, float from, float to)
    { if (to > from) Part(root, name, new Vector3(x, 1.5f, (from + to) / 2), new Vector3(.16f, 3, to - from), wall); }
    private static void CorridorWall(string name, float x, float from, float to, float[] doors)
    {
        float cursor = from;
        foreach (float center in doors)
        {
            WallX(name + "Wall", x, cursor, center - .6f);
            Part(root, name + "Header", new Vector3(x, 2.62f, center), new Vector3(.16f, .76f, 1.2f), wall);
            cursor = center + .6f;
        }
        WallX(name + "Wall", x, cursor, to);
    }
    private static void WindowWall(string name, float x, float from, float to, float center, bool high)
    {
        const float width = 1.8f;
        float bottom = high ? 1.65f : 1.05f, top = 2.5f;
        WallX(name + "Side", x, from, center - width / 2);
        WallX(name + "Side", x, center + width / 2, to);
        Part(root, name + "Below", new Vector3(x, bottom / 2, center), new Vector3(.16f, bottom, width), wall);
        Part(root, name + "Above", new Vector3(x, (top + 3) / 2, center), new Vector3(.16f, 3 - top, width), wall);
        Part(root, name + "Glass", new Vector3(x, (bottom + top) / 2, center), new Vector3(.02f, top - bottom, width), glass);
        foreach (float z in new[] { center - width / 2, center + width / 2, center })
            Part(root, name + "Frame", new Vector3(x, (bottom + top) / 2, z), new Vector3(.20f, top - bottom + .06f, .045f), trim);
        foreach (float y in new[] { bottom, top })
            Part(root, name + "Frame", new Vector3(x, y, center), new Vector3(.20f, .045f, width), trim);
    }
    private static void Door(string name, float x, float z, float side)
    {
        Transform hinge = new GameObject(name).transform; hinge.SetParent(root, false);
        hinge.position = new Vector3(x, 0, z - .54f);
        Part(hinge, "DoorLeaf", new Vector3(0, 1.08f, .54f), new Vector3(.055f, 2.16f, 1.08f), door);
        foreach (float face in new[] { -.065f, .065f })
            Part(hinge, "DoorHandle", new Vector3(face, 1.05f, .96f), new Vector3(.09f, .045f, .15f), trim);
        Openable openable = hinge.gameObject.AddComponent<Openable>();
        SerializedObject serialized = new SerializedObject(openable);
        serialized.FindProperty("displayName").stringValue = "die Tür";
        serialized.FindProperty("openAngle").floatValue = side < 0 ? -100 : 100;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (float edge in new[] { -.57f, .57f })
            Part(root, name + "Frame", new Vector3(x, 1.12f, z + edge), new Vector3(.22f, 2.24f, .05f), trim);
        Part(root, name + "FrameTop", new Vector3(x, 2.22f, z), new Vector3(.22f, .06f, 1.14f), trim);
    }
    private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static Material Mat(string name, Color color)
    {
        string path = "Assets/Materials/Home/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .15f); return material;
    }
}
