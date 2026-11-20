using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VisualPolishBuilder
{
    private const string Art = "Assets/Art/VisualPolish";
    private static Transform details;
    private static readonly Dictionary<string, Mesh> boxes = new Dictionary<string, Mesh>();
    private static Material plaster, oak, flooring, quartz, tile, painted, steel, ceramic, clearGlass;

    [MenuItem("Tools/DeutschRoom/Apply Visual Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Folder(Art); Folder(Art + "/Meshes");
        GameObject old = GameObject.Find("VisualPolishDetails"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
        details = new GameObject("VisualPolishDetails").transform;
        boxes.Clear();
        plaster = Surface("Plaster", "plaster", new Color(.91f, .9f, .85f), 0, .14f, .18f);
        oak = Surface("Oak", "oak", Color.white, 0, .29f, .22f);
        flooring = Surface("OakFloor", "floor", Color.white, 0, .32f, .25f);
        quartz = Surface("Quartz", "stone", Color.white, 0, .48f, .13f);
        tile = Surface("PorcelainTile", "tile", Color.white, 0, .38f, .18f);
        painted = Surface("PaintedCabinet", "plaster", new Color(.62f, .67f, .61f), 0, .36f, .045f);
        steel = Surface("BrushedSteel", "steel", new Color(.94f, .96f, .98f), .92f, .76f, .09f);
        ceramic = Simple("Ceramic", new Color(.94f, .93f, .88f), 0, .72f);
        clearGlass = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Glass.mat");
        if (clearGlass == null)
        { clearGlass = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WindowGlass.mat")); AssetDatabase.CreateAsset(clearGlass, Art + "/Glass.mat"); }
        clearGlass.SetColor("_BaseColor", new Color(.86f, .95f, 1, .25f)); clearGlass.SetFloat("_Smoothness", .95f); clearGlass.SetFloat("_Cull", 0);
        Material cloth = Surface("WovenCloth", "cloth", new Color(.31f, .45f, .48f), 0, .16f, .3f);
        ReplaceVisual("Schuessel_Bowl", "Bowl", ceramic);
        ReplaceVisual("Teller_Plate", "Plate", ceramic);
        ReplaceVisual("Glas_DrinkingGlass", "Glass", clearGlass);
        ReplaceVisual("Contents_WineGlass", "WineGlass", clearGlass);
        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
        {
            if (!renderer.enabled) continue;
            string name = renderer.name;
            if (name == "Plane" && renderer.bounds.center.y < .01f && renderer.bounds.size.x >= 9.9f)
            { renderer.enabled = false; continue; }
            string materialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
            bool home = renderer.transform.IsChildOf(GameObject.Find("HomeExpansion").transform);
            if (name == "Floor" || name.EndsWith("_Floor")) renderer.sharedMaterial = name.StartsWith("Bathroom") ? tile : flooring;
            else if (name == "Ceiling" || name.EndsWith("_Ceiling")) renderer.sharedMaterial = plaster;
            else if (name.StartsWith("Wall_") || name.StartsWith("WindowWall_") || name.StartsWith("DoorWall_") || materialPath == "Assets/Materials/Home/Wall.mat") renderer.sharedMaterial = plaster;
            else if (name.Contains("Handle") || name.Contains("Griff") || materialPath.EndsWith("/SinkSteel.mat") || materialPath == "Assets/Materials/Contents/Steel.mat") renderer.sharedMaterial = steel;
            else if (name.Contains("Counter") || materialPath.EndsWith("/KitchenStone.mat")) renderer.sharedMaterial = quartz;
            else if (name.Contains("Door") && !name.Contains("Fridge") && !name.Contains("Kuehlschrank") && !name.Contains("Backofen") && !name.Contains("Mikrowelle"))
                renderer.sharedMaterial = home || name == "Door_Leaf" ? oak : painted;
            else if (materialPath.EndsWith("/WoodBrown.mat") || materialPath == "Assets/Materials/Home/Door.mat") renderer.sharedMaterial = oak;
            else if (name.Contains("Chair_SeatCushion") || materialPath.EndsWith("/TowelBlue.mat")) renderer.sharedMaterial = cloth;
            else if (materialPath == "Assets/Materials/Contents/Ceramic.mat") renderer.sharedMaterial = ceramic;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && (filter.sharedMesh.name == "Cube" || filter.sharedMesh.name.StartsWith("Bevel_") || filter.sharedMesh.name.StartsWith("SoftEdge_")))
            {
                Vector3 size = Abs(filter.transform.lossyScale);
                if (Mathf.Min(size.x, size.y, size.z) > .004f) filter.sharedMesh = renderer.sharedMaterial == plaster ? FlatBox(size) : RoundedBox(size);
            }
        }
        AddArchitecturalDetails();
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional)
            { light.color = new Color(1, .94f, .84f); light.intensity = 1.8f; light.shadows = LightShadows.Soft; light.shadowStrength = .78f; light.shadowBias = .04f; light.shadowNormalBias = .22f; }
            else if (light.name == "AmbientRoomLight") { light.intensity = 1.4f; light.color = new Color(1, .96f, .90f); Vector3 p = light.transform.position; p.y = 2.25f; light.transform.position = p; }
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.62f, .67f, .70f);
        RenderSettings.ambientEquatorColor = new Color(.49f, .51f, .48f);
        RenderSettings.ambientGroundColor = new Color(.30f, .28f, .24f);
        RenderSettings.reflectionIntensity = .85f;
        Settings();
        BakeProbe();
        ObjectClarityBuilder.ApplyToOpenScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Debug.Log("VISUAL_POLISH_BUILD_PASS: textured PBR surfaces, physical-size UVs, softened edges, rebuilt tableware, trim, tile, warm lighting and native-resolution mobile rendering.");
    }
    public static void ApplyAndCapture()
    {
        Apply();
        EditorApplication.delayCall += () => { try { VisualReviewCapture.Capture("after"); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
    }
    private static void Settings()
    {
        foreach (string name in new[] { "Mobile", "PC" })
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/" + name + "_RPAsset.asset");
            pipeline.renderScale = 1;
            // Deferred PC rendering uses SMAA; forward mobile rendering uses 2x MSAA.
            pipeline.msaaSampleCount = name == "Mobile" ? 2 : 1;
            pipeline.supportsHDR = true; pipeline.shadowDistance = 32;
            SerializedObject settings = new SerializedObject(pipeline);
            Set(settings, "m_MainLightShadowmapResolution", 2048); Set(settings, "m_SoftShadowsSupported", true); Set(settings, "m_ShadowCascadeCount", 2);
            settings.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(pipeline);
        }
        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>())
        {
            var data = camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // Mobile uses MSAA; the camera component enables desktop SMAA.
            if (camera.GetComponent<VisualPolishCamera>() == null) camera.gameObject.AddComponent<VisualPolishCamera>();
        }
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        if (!profile.TryGet(out Tonemapping tone)) { tone = profile.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, profile); }
        tone.mode.Override(TonemappingMode.ACES);
        if (!profile.TryGet(out ColorAdjustments grading)) { grading = profile.Add<ColorAdjustments>(true); AssetDatabase.AddObjectToAsset(grading, profile); }
        grading.postExposure.Override(.4f); grading.contrast.Override(3); grading.saturation.Override(-4);
        if (profile.TryGet(out Bloom bloom)) bloom.intensity.Override(.10f);
        if (profile.TryGet(out Vignette vignette)) vignette.intensity.Override(.08f);
        EditorUtility.SetDirty(profile);
    }
    private static void Set(SerializedObject owner, string key, int value) { SerializedProperty p = owner.FindProperty(key); if (p != null) p.intValue = value; }
    private static void Set(SerializedObject owner, string key, bool value) { SerializedProperty p = owner.FindProperty(key); if (p != null) p.boolValue = value; }
    private static void BakeProbe()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        GameObject obj = new GameObject("KitchenReflection", typeof(ReflectionProbe)); obj.transform.SetParent(details, false); obj.transform.position = new Vector3(0, 1.4f, 1);
        ReflectionProbe probe = obj.GetComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Baked; probe.boxProjection = true;
        probe.size = new Vector3(10, 3, 10); probe.resolution = 128; probe.intensity = .85f; probe.hdr = true; probe.cullingMask = -1;
        if (!Lightmapping.BakeReflectionProbe(probe, Art + "/KitchenReflection.exr")) Debug.LogWarning("Kitchen reflection bake needs a graphics-enabled Unity editor.");
    }
    private static Material Simple(string name, Color color, float metallic, float smoothness)
    {
        string path = Art + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness); return material;
    }
    private static Material Surface(string name, string kind, Color tint, float metallic, float smoothness, float bump)
    {
        Texture2D albedo = Texture(kind, false), normal = Texture(kind, true);
        Material material = Simple(name, tint, metallic, smoothness);
        material.SetTexture("_BaseMap", albedo); material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", bump); material.EnableKeyword("_NORMALMAP"); return material;
    }
    private static float Noise(float u, float v, float scale)
    {
        float a = Mathf.Lerp(Mathf.PerlinNoise(u * scale, v * scale), Mathf.PerlinNoise((u - 1) * scale, v * scale), u);
        float b = Mathf.Lerp(Mathf.PerlinNoise(u * scale, (v - 1) * scale), Mathf.PerlinNoise((u - 1) * scale, (v - 1) * scale), u);
        return Mathf.Lerp(a, b, v);
    }
    private static float Grain(float u, float v, float xScale, float yScale)
    {
        float a = Mathf.Lerp(Mathf.PerlinNoise(u * xScale, v * yScale), Mathf.PerlinNoise((u - 1) * xScale, v * yScale), u);
        float b = Mathf.Lerp(Mathf.PerlinNoise(u * xScale, (v - 1) * yScale), Mathf.PerlinNoise((u - 1) * xScale, (v - 1) * yScale), u);
        return Mathf.Lerp(a, b, v);
    }
    private static Color Pixel(string kind, float u, float v, out float height)
    {
        float noise = Noise(u, v, 48), broad = Noise(u, v, 8); height = noise * .06f;
        switch (kind)
        {
            case "oak": case "floor":
                float grain = (Grain(u, v, 120, 5) - .5f) * .09f + (Grain(u, v, 40, 9) - .5f) * .035f;
                float plank = Mathf.Floor(u * 6), seam = 1;
                if (kind == "floor")
                { if (Mathf.Repeat(u * 6, 1) < .012f || Mathf.Repeat(v + plank * .37f, 1) < .008f) seam = .62f; grain += Mathf.Sin(plank * 2.1f) * .023f; }
                height = grain * .35f + (seam - 1) * .15f;
                return new Color(.67f + grain, .55f + grain * .85f, .41f + grain * .65f) * seam;
            case "stone":
                float vein = Mathf.Pow(Mathf.Abs(Mathf.Sin((u * 2 + v) * Mathf.PI * 4 + broad * 5)), 24) * .10f;
                height = noise * .015f; return new Color(.79f + noise * .04f - vein, .79f + noise * .04f - vein, .75f + noise * .04f - vein);
            case "tile":
                bool grout = Mathf.Repeat(u * 3, 1) < .014f || Mathf.Repeat(v * 3, 1) < .014f;
                height = grout ? 0 : .12f;
                return grout ? new Color(.45f, .48f, .46f) : new Color(.77f, .80f, .77f) * (.96f + noise * .06f);
            case "steel": height = Noise(u, v, 120) * .025f; return Color.white * (.78f + Mathf.Sin(u * Mathf.PI * 400) * .02f + noise * .02f);
            case "cloth": height = (Mathf.Sin(u * Mathf.PI * 256) + Mathf.Sin(v * Mathf.PI * 256)) * .05f; return Color.white * (.86f + noise * .14f);
            default: height = noise * .05f; return Color.white * (.96f + noise * .04f);
        }
    }
    private static Texture2D Texture(string kind, bool normal)
    {
        string path = Art + "/" + kind + (normal ? "-normal" : "-albedo") + ".png";
        if (!File.Exists(path) || kind == "oak" || kind == "floor")
        {
            const int size = 512; Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size; Color color = Pixel(kind, u, v, out float h);
                if (normal)
                {
                    Pixel(kind, Mathf.Repeat(u + 1f / size, 1), v, out float right);
                    Pixel(kind, u, Mathf.Repeat(v + 1f / size, 1), out float up);
                    Vector3 n = new Vector3((h - right) * 4, (h - up) * 4, 1).normalized;
                    color = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f);
                }
                texture.SetPixel(x, y, color);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal; importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
            importer.maxTextureSize = 512; importer.mipmapEnabled = true; importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Vector3 Abs(Vector3 p) => new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
    public static Mesh FurnitureBox(Vector3 size) => RoundedBox(size);
    private static Mesh RoundedBox(Vector3 size)
    {
        string key = Mathf.RoundToInt(size.x * 10000) + "_" + Mathf.RoundToInt(size.y * 10000) + "_" + Mathf.RoundToInt(size.z * 10000);
        if (boxes.TryGetValue(key, out Mesh cached)) return cached;
        string path = Art + "/Meshes/Bevel_" + key + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            float radius = Mathf.Min(.008f, Mathf.Min(size.x, size.y, size.z) * .08f);
            Vector3 half = size / 2, inner = half - Vector3.one * radius;
            List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>(); List<Vector2> uv = new List<Vector2>(); List<int> triangles = new List<int>();
            for (int axis = 0; axis < 3; axis++) foreach (int side in new[] { -1, 1 })
            {
                int a = (axis + 1) % 3, b = (axis + 2) % 3, start = vertices.Count;
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                {
                    Vector3 p = Vector3.zero; p[axis] = side * half[axis]; p[a] = Edge(x, half[a], radius); p[b] = Edge(y, half[b], radius);
                    Vector3 core = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 n = (p - core).normalized, rounded = core + n * radius;
                    vertices.Add(new Vector3(rounded.x / size.x, rounded.y / size.y, rounded.z / size.z)); normals.Add(Vector3.Scale(n, size).normalized);
                    uv.Add(new Vector2(p[a], p[b]));
                }
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    int i = start + y * 4 + x;
                    if (side > 0) triangles.AddRange(new[] { i, i + 1, i + 4, i + 1, i + 5, i + 4 });
                    else triangles.AddRange(new[] { i, i + 4, i + 1, i + 1, i + 4, i + 5 });
                }
            }
            mesh = new Mesh { name = "SoftEdge_" + key }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh, path);
        }
        boxes[key] = mesh; return mesh;
    }
    private static Mesh FlatBox(Vector3 size)
    {
        string key = Mathf.RoundToInt(size.x * 10000) + "_" + Mathf.RoundToInt(size.y * 10000) + "_" + Mathf.RoundToInt(size.z * 10000);
        string path = Art + "/Meshes/SurfaceBox_" + key + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh != null) return mesh;
        GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh = UnityEngine.Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.DestroyImmediate(temporary);
        Vector3[] vertices = mesh.vertices, normals = mesh.normals; Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = Vector3.Scale(vertices[i], size), n = normals[i];
            uv[i] = Mathf.Abs(n.y) > .5f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > .5f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
        }
        mesh.name = "SurfaceBox_" + key; mesh.uv = uv; mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    private static float Edge(int index, float half, float radius) => index == 0 ? -half : index == 1 ? -half + radius : index == 2 ? half - radius : half;
    private static void ReplaceVisual(string objectName, string shape, Material material)
    {
        GameObject item = GameObject.Find(objectName); if (item == null) return;
        Bounds bounds = new Bounds(); bool first = true;
        foreach (MeshFilter filter in item.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.name == "RefinedVisual") continue;
            Bounds b = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = item.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
            Renderer r = filter.GetComponent<Renderer>(); if (r != null) r.enabled = false;
        }
        Transform old = item.transform.Find("RefinedVisual"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        Vector2[] profile = shape == "Bowl" ? new[] { new Vector2(0, 0), new Vector2(.42f, 0), new Vector2(.62f, .18f), new Vector2(.84f, .55f), new Vector2(1, .95f), new Vector2(1, 1), new Vector2(.93f, 1), new Vector2(.78f, .55f), new Vector2(.51f, .18f), new Vector2(0, .12f) } :
            shape == "Plate" ? new[] { new Vector2(0, 0), new Vector2(.65f, 0), new Vector2(.84f, .25f), new Vector2(1, .8f), new Vector2(1, 1), new Vector2(.96f, 1), new Vector2(.75f, .4f), new Vector2(0, .35f) } :
            shape == "WineGlass" ? new[] { new Vector2(0, 0), new Vector2(.82f, 0), new Vector2(.82f, .04f), new Vector2(.1f, .09f), new Vector2(.1f, .45f), new Vector2(.6f, .55f), new Vector2(1, .8f), new Vector2(.82f, 1), new Vector2(.76f, 1), new Vector2(.93f, .8f), new Vector2(.48f, .58f), new Vector2(0, .5f) } :
            new[] { new Vector2(0, 0), new Vector2(.9f, 0), new Vector2(1, .99f), new Vector2(.99f, 1), new Vector2(.91f, 1), new Vector2(.83f, .08f), new Vector2(0, .08f) };
        string path = Art + "/Meshes/Refined_" + shape + ".asset"; Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            const int segments = 64; List<Vector3> vertices = new List<Vector3>(); List<Vector2> uv = new List<Vector2>(); List<int> triangles = new List<int>();
            for (int j = 0; j < profile.Length; j++) for (int i = 0; i <= segments; i++)
            { float angle = i * Mathf.PI * 2 / segments; vertices.Add(new Vector3(Mathf.Cos(angle) * profile[j].x, profile[j].y, Mathf.Sin(angle) * profile[j].x)); uv.Add(new Vector2((float)i / segments, (float)j / (profile.Length - 1))); }
            for (int j = 0; j < profile.Length - 1; j++) for (int i = 0; i < segments; i++)
            { int a = j * (segments + 1) + i, b = a + segments + 1; triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            mesh = new Mesh { name = "Refined_" + shape }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh, path);
        }
        GameObject visual = new GameObject("RefinedVisual", typeof(MeshFilter), typeof(MeshRenderer)); visual.transform.SetParent(item.transform, false);
        visual.transform.localPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        visual.transform.localScale = new Vector3(bounds.extents.x, bounds.size.y, bounds.extents.z);
        visual.GetComponent<MeshFilter>().sharedMesh = mesh; visual.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static void AddArchitecturalDetails()
    {
        foreach (BoxCollider part in UnityEngine.Object.FindObjectsByType<BoxCollider>())
        {
            Bounds b = part.bounds; string name = part.name;
            if (b.min.y > .05f || b.size.y < .5f || part.GetComponentInParent<KitchenInteractable>() != null) continue;
            if (!(name.Contains("Wall") || name.Contains("Divider") || name.Contains("Window") || part.transform.parent?.name == "HomeExpansion")) continue;
            bool x = b.size.x > b.size.z;
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 center = new Vector3(b.center.x, .045f, b.center.z);
                if (x) center.z += side * (b.extents.z + .008f); else center.x += side * (b.extents.x + .008f);
                Detail("Sockelleiste_Baseboard", center, x ? new Vector3(b.size.x, .09f, .016f) : new Vector3(.016f, .09f, b.size.z), ceramic);
            }
        }
        Detail("BathTile_South", new Vector3(6.65f, .6f, -15.91f), new Vector3(5.5f, 1.2f, .012f), tile);
        Detail("BathTile_North", new Vector3(6.65f, .6f, -11.09f), new Vector3(5.5f, 1.2f, .012f), tile);
        Detail("BathTile_East", new Vector3(9.41f, .6f, -13.5f), new Vector3(.012f, 1.2f, 4.8f), tile);
        Detail("BathTile_WestA", new Vector3(3.89f, .6f, -15), new Vector3(.012f, 1.2f, 1.8f), tile);
        Detail("BathTile_WestB", new Vector3(3.89f, .6f, -11.9f), new Vector3(.012f, 1.2f, 1.6f), tile);
    }
    private static void Detail(string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(details, false);
        obj.transform.position = position; obj.transform.localScale = size;
        obj.GetComponent<MeshFilter>().sharedMesh = RoundedBox(size); obj.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
