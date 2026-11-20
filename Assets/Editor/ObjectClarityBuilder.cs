using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Replace only renderers: existing pickup IDs, poses, colliders and vocabulary owners survive.
public static class ObjectClarityBuilder
{
    private const string Art = "Assets/Art/ObjectClarity";
    private const string Visual = "RecognizableVisual";
    private static readonly string[] Names = { "Loeffel_Spoon", "Gabel_Fork", "Messer_Knife", "Contents_Apple", "Contents_Tomato" };
    private static Material metal, grip, apple, tomato, leaf, stem;

    [MenuItem("Tools/DeutschRoom/Improve Object Readability")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        ApplyToOpenScene();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Validate();
        AssetDatabase.SaveAssets();
        Debug.Log("OBJECT_CLARITY_PASS: five existing targets refined; identities, colliders and progression preserved.");
    }

    public static void Validate()
    {
        if (UnityEngine.Object.FindObjectsByType<PickupItem>().Length != 53 ||
            UnityEngine.Object.FindObjectsByType<FixedLearningTarget>().Length != 80)
            throw new InvalidOperationException("Learning target populations changed.");
        foreach (string name in Names)
        {
            GameObject item = GameObject.Find(name);
            if (item.GetComponent<PickupItem>() == null || item.GetComponentsInChildren<Collider>().Length == 0)
                throw new InvalidOperationException("Pickup interaction missing: " + name);
            Transform visual = item.transform.Find(Visual);
            if (visual == null || visual.GetComponentsInChildren<Collider>().Length > 0)
                throw new InvalidOperationException("Visual replacement changed physics: " + name);
            foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || mesh.vertexCount < 3 || Vector3.Scale(mesh.bounds.size, filter.transform.localScale).magnitude > .4f)
                    throw new InvalidOperationException("Invalid object mesh: " + name);
                foreach (Vector3 vertex in mesh.vertices)
                    if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsNaN(vertex.z))
                        throw new InvalidOperationException("Non-finite mesh: " + name);
            }
        }
        Mesh bowl = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "/SpoonConcaveBowl.asset");
        if (bowl == null || bowl.normals[200].y < .5f) throw new InvalidOperationException("Spoon interior faces inward.");
        Debug.Log("OBJECT_CLARITY_VALIDATION_PASS: meshes, bowl normals, pickup colliders and 53/80 targets.");
    }

    public static void ApplyToOpenScene()
    {
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        // A lighter, less mirror-dependent finish remains readable under indoor/mobile lighting.
        metal = Material("SatinSilver", new Color(.82f, .87f, .92f), .48f, .6f);
        grip = Material("KnifeGrip", new Color(.065f, .09f, .12f), .05f, .3f);
        apple = Material("AppleSkin", new Color(.72f, .055f, .025f), 0, .36f);
        tomato = Material("TomatoSkin", new Color(.95f, .18f, .035f), 0, .32f);
        leaf = Material("Leaf", new Color(.16f, .39f, .045f), 0, .23f);
        leaf.SetFloat("_Cull", 0);
        leaf.SetColor("_EmissionColor", new Color(.018f, .045f, .004f));
        leaf.EnableKeyword("_EMISSION");
        stem = Material("Stem", new Color(.23f, .105f, .035f), 0, .2f);
        Skin(apple, "AppleSkin", true); Skin(tomato, "TomatoSkin", false);
        foreach (string name in Names)
        {
            GameObject item = GameObject.Find(name);
            if (item == null) throw new InvalidOperationException("Missing target " + name);
            Transform old = item.transform.Find(Visual);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            Transform root = new GameObject(Visual).transform;
            root.SetParent(item.transform, false);
            if (name == Names[0]) Spoon(root);
            else if (name == Names[1]) Fork(root);
            else if (name == Names[2]) Knife(root);
            else Fruit(root, name == Names[3]);
        }
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        string path = Art + "/" + name + ".mat";
        Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result == null) { result = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result, path); }
        result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", metallic); result.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(result);
        return result;
    }

    private static void Skin(Material material, string name, bool isApple)
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float u = (float)x / size, v = (float)y / size;
            float noise = Mathf.PerlinNoise(u * 24, v * 16);
            float stripe = Mathf.Pow(.5f + .5f * Mathf.Sin(u * Mathf.PI * 68 + noise * 3), 6);
            float fleck = Mathf.PerlinNoise(u * 210, v * 170) > .78f ? .12f : 0;
            float light = .83f + noise * .17f + (isApple ? stripe * .16f + fleck : 0);
            texture.SetPixel(x, y, new Color(light, light, light));
        }
        texture.Apply();
        string path = Art + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
    }

    private static void MeshPart(Transform root, string name, List<Vector3> vertices, List<int> indices, Material material, List<Vector2> uv = null)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0);
        if (uv != null) mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Art + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
        EditorUtility.SetDirty(mesh);
        GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(root, false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<Renderer>().sharedMaterial = material;
        if (material == leaf)
        {
            // Tiny leaves otherwise self-shadow entirely under the house's large architectural shadow bias.
            part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            part.GetComponent<Renderer>().receiveShadows = false;
        }
    }

    // Rounded elliptical cross sections give cutlery visible thickness and soft highlights.
    private static void Sweep(Transform root, string name, Vector4[] sections, Material material, float offsetX = 0)
    {
        const int sides = 12;
        List<Vector3> vertices = new List<Vector3>(); List<int> indices = new List<int>();
        foreach (Vector4 section in sections) for (int j = 0; j <= sides; j++)
        {
            float angle = j * Mathf.PI * 2 / sides;
            vertices.Add(new Vector3(offsetX + section.z * Mathf.Cos(angle), section.y + section.w * Mathf.Sin(angle), section.x));
        }
        for (int i = 0; i < sections.Length - 1; i++) for (int j = 0; j < sides; j++)
        { int a = i * (sides + 1) + j, b = a + sides + 1; indices.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
        MeshPart(root, name, vertices, indices, material);
    }

    private static Vector4 S(float z, float y, float halfWidth, float halfThickness) => new Vector4(z, y, halfWidth, halfThickness);

    private static void Handle(Transform root, string name, float tip, Material material)
    {
        Sweep(root, name, new[] { S(-.112f,.006f,0,0), S(-.107f,.006f,.010f,.003f), S(-.09f,.006f,.012f,.0035f),
            S(-.055f,.006f,.009f,.003f), S(-.015f,.008f,.006f,.0025f), S(tip,.011f,.005f,.002f) }, material);
    }

    private static void Spoon(Transform root)
    {
        Handle(root, "SpoonHandle", .031f, metal);
        const int rings = 12, sides = 48;
        List<Vector3> vertices = new List<Vector3>(); List<int> indices = new List<int>();
        // Inner concavity and outer shell join at a rounded rim; bowl is never a flattened sphere.
        for (int layer = 0; layer < 2; layer++) for (int ring = 0; ring <= rings; ring++) for (int j = 0; j <= sides; j++)
        {
            float r = Mathf.Max(.0001f, (float)ring / rings), a = j * Mathf.PI * 2 / sides;
            float x = Mathf.Cos(a) * .026f * r, z = .066f + Mathf.Sin(a) * .039f * r;
            float y = .003f + .015f * r * r - (layer == 1 ? .0022f * (1 - .2f * r) : 0);
            vertices.Add(new Vector3(x, y, z));
        }
        int surface = (rings + 1) * (sides + 1);
        for (int layer = 0; layer < 2; layer++) for (int ring = 0; ring < rings; ring++) for (int j = 0; j < sides; j++)
        {
            int a = layer * surface + ring * (sides + 1) + j, b = a + sides + 1;
            if (layer == 0) indices.AddRange(new[] { a,a+1,b,a+1,b+1,b });
            else indices.AddRange(new[] { a,b,a+1,a+1,b,b+1 });
        }
        for (int j = 0; j < sides; j++)
        { int a = rings * (sides + 1) + j, b = a + surface; indices.AddRange(new[] { a,a+1,b,a+1,b+1,b }); }
        MeshPart(root, "SpoonConcaveBowl", vertices, indices, metal);
    }

    private static void Fork(Transform root)
    {
        Handle(root, "ForkHandle", .018f, metal);
        Sweep(root, "ForkShoulder", new[] { S(.012f,.010f,.005f,.0025f), S(.026f,.012f,.018f,.0025f), S(.044f,.011f,.021f,.002f), S(.046f,.011f,.021f,.002f) }, metal);
        for (int i = 0; i < 4; i++)
            Sweep(root, "ForkTine" + i, new[] { S(.041f,.011f,.0032f,.002f), S(.061f,.010f,.0030f,.0018f),
                S(.088f,.014f,.0021f,.0015f), S(.101f,.018f,.0004f,.0005f) }, metal, (i - 1.5f) * .012f);
    }

    private static void Knife(Transform root)
    {
        Handle(root, "KnifeHandle", -.007f, grip);
        Sweep(root, "KnifeBlade", new[] { S(-.012f,.009f,.009f,.002f), S(.002f,.009f,.014f,.002f),
            S(.065f,.009f,.015f,.0017f), S(.09f,.009f,.010f,.0013f), S(.105f,.009f,0,0) }, metal);
        for (int i = 0; i < 2; i++)
        {
            GameObject rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere); rivet.name = "KnifeRivet";
            rivet.transform.SetParent(root, false); rivet.transform.localPosition = new Vector3(0,.0095f,-.086f + i * .043f);
            rivet.transform.localScale = new Vector3(.004f,.0015f,.004f); rivet.GetComponent<Renderer>().sharedMaterial = metal;
            UnityEngine.Object.DestroyImmediate(rivet.GetComponent<Collider>());
        }
    }

    private static void Fruit(Transform root, bool isApple)
    {
        const int rings = 32, sides = 48;
        List<Vector3> vertices = new List<Vector3>(); List<Vector2> uv = new List<Vector2>(); List<int> indices = new List<int>();
        for (int i = 0; i <= rings; i++) for (int j = 0; j <= sides; j++)
        {
            float t = i * Mathf.PI / rings, a = j * Mathf.PI * 2 / sides;
            float radius = Mathf.Sin(t) * (isApple ? .066f : .065f);
            if (isApple) radius *= 1 + .045f * Mathf.Cos(5 * a) * Mathf.Pow(Mathf.Abs(Mathf.Cos(t)), 3) + .09f * Mathf.Cos(t);
            float y = .061f + Mathf.Cos(t) * (isApple ? .06f : .046f);
            if (isApple) y -= .014f * Mathf.Exp(-t * t / .09f);
            vertices.Add(new Vector3(radius * Mathf.Cos(a), y, radius * Mathf.Sin(a)));
            uv.Add(new Vector2((float)j / sides, (float)i / rings));
        }
        for (int i = 0; i < rings; i++) for (int j = 0; j < sides; j++)
        { int a = i * (sides + 1) + j, b = a + sides + 1; indices.AddRange(new[] { a,a+1,b,a+1,b+1,b }); }
        MeshPart(root, isApple ? "AppleLobedBody" : "TomatoFlattenedBody", vertices, indices, isApple ? apple : tomato, uv);
        Vector3 bottom = new Vector3(0, .104f, 0);
        Vector3 top = isApple ? new Vector3(.008f, .144f, .003f) : new Vector3(0, .126f, 0);
        GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stalk.name = isApple ? "AppleStem" : "TomatoStem"; stalk.transform.SetParent(root, false);
        stalk.transform.localPosition = (bottom + top) / 2;
        stalk.transform.localRotation = Quaternion.FromToRotation(Vector3.up, top - bottom);
        stalk.transform.localScale = new Vector3(.006f, (top - bottom).magnitude / 2, .006f);
        stalk.GetComponent<Renderer>().sharedMaterial = isApple ? stem : leaf;
        UnityEngine.Object.DestroyImmediate(stalk.GetComponent<Collider>());
        if (isApple) Leaf(root, "AppleLeaf", new Vector3(.002f,.125f,0), 0, .045f, .013f);
        else for (int i = 0; i < 5; i++) Leaf(root, "TomatoSepal" + i, new Vector3(0,.107f,0), i * Mathf.PI * 2 / 5, .035f, .007f);
    }

    private static void Leaf(Transform root, string name, Vector3 origin, float angle, float length, float width)
    {
        List<Vector3> vertices = new List<Vector3>(); List<int> indices = new List<int>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f, half = Mathf.Sin(t * Mathf.PI) * width;
            for (int j = -1; j <= 1; j++)
            {
                Vector3 p = new Vector3(length * t, .008f * Mathf.Sin(t * Mathf.PI) - Mathf.Abs(j) * .003f, half * j);
                vertices.Add(origin + Quaternion.Euler(0,angle * Mathf.Rad2Deg,0) * p);
            }
        }
        for (int i = 0; i < 8; i++) for (int j = 0; j < 2; j++)
        { int a = i * 3 + j, b = a + 3; indices.AddRange(new[] { a,a+1,b,a+1,b+1,b }); }
        MeshPart(root, name, vertices, indices, leaf);
    }

    public static void ApplyAndCapture()
    {
        Apply();
        StartCapture("after");
    }

    public static void CaptureBefore()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        StartCapture("before");
    }

    private static void StartCapture(string suffix)
    {
        IEnumerator frames = Capture(suffix);
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (frames.MoveNext()) return;
                EditorApplication.update -= tick; EditorApplication.Exit(0);
            }
            catch (Exception exception)
            { EditorApplication.update -= tick; Debug.LogException(exception); EditorApplication.Exit(1); }
        };
        EditorApplication.update += tick;
    }

    private static IEnumerator Capture(string suffix)
    {
        yield return null;
        yield return null;
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Graphics required.");
        Camera camera = new GameObject("Object clarity review", typeof(Camera)).GetComponent<Camera>();
        camera.enabled = false; camera.fieldOfView = 38; camera.nearClipPlane = .01f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        RenderTexture target = new RenderTexture(1000, 800, 24);
        target.Create();
        Directory.CreateDirectory("Documentation/VisualReview");
        var hidden = new Dictionary<Renderer, bool>();
        foreach (PickupItem pickup in UnityEngine.Object.FindObjectsByType<PickupItem>())
            foreach (Renderer renderer in pickup.GetComponentsInChildren<Renderer>())
            { hidden[renderer] = renderer.enabled; renderer.enabled = false; }
        foreach (string name in Names)
        {
            Transform item = GameObject.Find(name).transform;
            // A temporary studio arrangement avoids refrigerator walls hiding the fruit during review.
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>()) renderer.enabled = hidden[renderer];
            Vector3 old = item.position; Quaternion rotation = item.rotation;
            item.SetPositionAndRotation(new Vector3(0,.93f,1), Quaternion.identity);
            Vector3 focus = item.position + (name.Contains("Contents") ? Vector3.up * .07f : Vector3.zero);
            camera.transform.position = focus + new Vector3(.12f,.26f,-.25f); camera.transform.LookAt(focus);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            Texture2D image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
            File.WriteAllBytes("Documentation/VisualReview/clarity-" + name + "-" + suffix + ".png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image); RenderTexture.active = previous;
            item.SetPositionAndRotation(old,rotation);
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            yield return null;
        }
        foreach (var pair in hidden) pair.Key.enabled = pair.Value;
        target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(camera.gameObject);
        Debug.Log("OBJECT_CLARITY_CAPTURE_PASS");
    }
}
