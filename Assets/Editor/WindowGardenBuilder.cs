using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class WindowGardenBuilder
{
    private static Transform root;
    private static Material grass, leaves, lightLeaves, bark, stone, path, flower, white, wood;
    [MenuItem("Tools/DeutschRoom/Rebuild Window Garden")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        GameObject old = GameObject.Find("WindowGarden");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Garden")) AssetDatabase.CreateFolder("Assets/Materials", "Garden");
        grass = Mat("Grass", new Color(.27f, .46f, .20f)); leaves = Mat("Leaves", new Color(.12f, .32f, .17f));
        lightLeaves = Mat("LightLeaves", new Color(.38f, .56f, .23f)); bark = Mat("Bark", new Color(.29f, .19f, .12f));
        stone = Mat("Stone", new Color(.53f, .57f, .53f)); path = Mat("Path", new Color(.72f, .66f, .51f));
        flower = Mat("Flowers", new Color(.94f, .62f, .16f)); white = Mat("WhiteFlowers", new Color(.96f, .91f, .78f));
        wood = Mat("BenchWood", new Color(.46f, .29f, .16f));
        root = new GameObject("WindowGarden").transform;
        Part("Gras_Grass", PrimitiveType.Cube, new Vector3(-18, -.14f, .4f), new Vector3(25, .22f, 32), grass);
        Part("Gartenweg_Path", PrimitiveType.Cube, new Vector3(-8.8f, -.022f, .4f), new Vector3(1.15f, .025f, 28), path);
        Part("Gartenweg_Path", PrimitiveType.Cube, new Vector3(-14, -.02f, 5.5f), new Vector3(10, .025f, 1.2f), path);
        Tree(new Vector3(-11.2f, 0, .6f), 1.05f);
        Tree(new Vector3(-9.8f, 0, -4.2f), .85f);
        Tree(new Vector3(-13, 0, 5.4f), 1.2f);
        Tree(new Vector3(-17, 0, -3), 1.4f);
        Tree(new Vector3(-19, 0, 7.5f), 1.1f);
        for (int i = 0; i < 9; i++)
            Part("Strauch_Shrub", PrimitiveType.Sphere, new Vector3(-7.1f - i % 2 * .5f, .34f, -4.5f + i * 1.2f), new Vector3(1.25f, .85f, 1.1f), i % 2 == 0 ? leaves : lightLeaves);
        for (int i = 0; i < 22; i++)
        {
            Vector3 at = new Vector3(-6.5f - .7f * (i % 3), 0, -3.3f + i * .35f);
            Part("Blume_Stem", PrimitiveType.Cylinder, at + Vector3.up * .2f, new Vector3(.025f, .2f, .025f), leaves);
            Part("Blume_Flower", PrimitiveType.Sphere, at + Vector3.up * .42f, new Vector3(.15f, .09f, .15f), i % 3 == 0 ? white : flower);
        }
        for (int i = 0; i < 6; i++)
            Part("Stein_Stone", PrimitiveType.Sphere, new Vector3(-7.7f, .08f, -2.2f + i * .8f), new Vector3(.35f, .2f, .27f), stone);
        Part("Huegel_Hill", PrimitiveType.Sphere, new Vector3(-29, -.4f, -5), new Vector3(9, 6, 22), lightLeaves);
        Part("Huegel_Hill", PrimitiveType.Sphere, new Vector3(-29, -.4f, 8), new Vector3(10, 8, 20), leaves);
        Vector3 bench = new Vector3(-10.4f, 0, 3.3f);
        Part("Gartenbank_Seat", PrimitiveType.Cube, bench + Vector3.up * .48f, new Vector3(.52f, .08f, 1.7f), wood);
        Part("Gartenbank_Back", PrimitiveType.Cube, bench + new Vector3(-.23f, .8f, 0), new Vector3(.06f, .58f, 1.7f), wood);
        foreach (float z in new[] { -.62f, .62f }) foreach (float x in new[] { -.17f, .17f })
            Part("Gartenbank_Leg", PrimitiveType.Cube, bench + new Vector3(x, .23f, z), new Vector3(.07f, .46f, .07f), bark);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        const string section = "مرحله ۱۱ — فضای سبز بیرون پنجره";
        string docs = File.ReadAllText("Documentation/tools.txt");
        int start = docs.IndexOf(section, StringComparison.Ordinal);
        string laterSections = "";
        if (start >= 0)
        {
            int next = docs.IndexOf("\nمرحله ", start + section.Length, StringComparison.Ordinal);
            if (next >= 0) laterSections = "\n" + docs.Substring(next);
            docs = docs.Substring(0, start).TrimEnd();
        }
        File.WriteAllText("Documentation/tools.txt", docs.TrimEnd() + "\n\n" + section + "\nDeutsch | فارسی | English\n" +
            "der Baum | درخت | tree\ndas Gras | چمن | grass\nder Strauch | بوته | shrub\ndie Blume | گل | flower\nder Gartenweg | مسیر باغ | garden path\ndie Gartenbank | نیمکت باغ | garden bench\nder Hügel | تپه | hill\nder Stein | سنگ | stone\n\nفضای بیرون تزئینی است و از پنجره دیده می‌شود؛ وسایل قابل برداشتن نیستند.\n" + laterSections);
        Debug.Log("WINDOW_GARDEN_BUILD_PASS: garden, trees, shrubs, flowers, path, bench and distant hills outside the west window.");
    }
    private static void Tree(Vector3 at, float scale)
    {
        Part("Baum_Trunk", PrimitiveType.Cylinder, at + Vector3.up * (1.3f * scale), new Vector3(.3f, 1.3f, .3f) * scale, bark);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI / 2;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * .65f, 2.7f + i * .18f, Mathf.Sin(angle) * .65f);
            Part("Baum_Canopy", PrimitiveType.Sphere, at + offset * scale, new Vector3(2.5f, 2.15f, 2.2f) * scale, i % 2 == 0 ? leaves : lightLeaves);
        }
    }
    private static void Part(string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type); obj.name = name;
        obj.transform.SetParent(root, false); obj.transform.position = position; obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        // Outside the playable room: decorative meshes need no physics or marker hit targets.
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    private static Material Mat(string name, Color color)
    {
        string file = "Assets/Materials/Garden/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(file);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, file); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .12f); return material;
    }
}
