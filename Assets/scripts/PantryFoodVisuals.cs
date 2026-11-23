using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Presentation only: keep the authored pickup root, colliders, vocabulary and save identity.
public sealed class PantryFoodVisuals : MonoBehaviour
{
    private readonly List<Object> owned = new List<Object>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

    private static void Install()
    {
        foreach (string id in new[] { "Contents_Rice", "Contents_Pasta", "Contents_Coffee" })
        {
            GameObject item = GameObject.Find(id);
            if (item != null && item.GetComponent<PantryFoodVisuals>() == null)
                item.AddComponent<PantryFoodVisuals>().Build(id);
        }
    }

    private void Build(string id)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        Transform visual = new GameObject("Pantry food visual").transform;
        visual.SetParent(transform, false);
        bool rice = id == "Contents_Rice", pasta = id == "Contents_Pasta";
        Material food = Surface("Food", rice ? new Color(.95f,.89f,.70f) : pasta ? new Color(.94f,.65f,.16f) : new Color(.22f,.085f,.025f), .24f);
        Material edge = Surface("Jar rim", new Color(.57f,.65f,.66f), .7f);
        Material lid = Surface("Distinctive lid", rice ? new Color(.25f,.48f,.40f) : pasta ? new Color(.65f,.22f,.08f) : new Color(.18f,.12f,.08f), .35f);
        var contents = new Geometry();
        var grooves = new Geometry();
        // Local deterministic sampling never changes Unity's gameplay random state.
        var random = new System.Random(rice ? 71 : pasta ? 83 : 97);
        int count = rice ? 190 : pasta ? 42 : 90;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            float radius = Mathf.Sqrt((float)random.NextDouble()) * (pasta ? .044f : .050f);
            Vector3 center = new Vector3(Mathf.Cos(angle)*radius, .016f + (float)random.NextDouble()*.142f, Mathf.Sin(angle)*radius);
            Quaternion rotation = Quaternion.Euler((float)random.NextDouble()*180, (float)random.NextDouble()*360, (float)random.NextDouble()*180);
            if (pasta)
                contents.Tube(center, rotation, .0055f, .0032f, .026f);
            else
            {
                Vector3 dimensions = rice ? new Vector3(.0023f,.007f,.002f) : new Vector3(.006f,.008f,.004f);
                contents.Ellipsoid(center, rotation, dimensions);
                if (!rice)
                {
                    // A dark, slightly curved seam makes each bean distinguishable from cereal.
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 offset = new Vector3(Mathf.Sin(j*1.5f)*.0006f, (j-1)*.004f, -.0038f);
                        grooves.Ellipsoid(center + rotation*offset, rotation, new Vector3(.0007f,.003f,.0006f));
                    }
                }
            }
        }
        Draw(visual, "Visible contents", contents, food);
        if (!rice && !pasta) Draw(visual, "Coffee bean seams", grooves, Surface("Roast seam", new Color(.055f,.018f,.006f), .15f));
        var rims = new Geometry();
        rims.Tube(new Vector3(0,.007f,0), Quaternion.identity, .064f,.058f,.010f);
        rims.Tube(new Vector3(0,.176f,0), Quaternion.identity, .064f,.059f,.006f);
        Draw(visual,"Glass jar rims",rims,edge);
        var cap = new Geometry();
        cap.Tube(new Vector3(0,.187f,0),Quaternion.identity,.069f,0,.016f);
        Draw(visual,"Screw lid",cap,lid);
        var glass = new Geometry();
        glass.Tube(new Vector3(0,.09f,0),Quaternion.identity,.063f,.0618f,.16f);
        Material clear = Surface("Clear glass",new Color(.78f,.92f,.96f,.10f),.82f);
        clear.SetFloat("_Surface",1); clear.SetFloat("_Blend",0);
        clear.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); clear.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        clear.SetFloat("_ZWrite",0); clear.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        clear.SetOverrideTag("RenderType","Transparent"); clear.renderQueue = (int)RenderQueue.Transparent;
        Draw(visual,"Transparent jar",glass,clear).shadowCastingMode = ShadowCastingMode.Off;
    }

    private Material Surface(string label, Color color, float smoothness)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label };
        material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",smoothness);
        owned.Add(material); return material;
    }

    private MeshRenderer Draw(Transform root, string label, Geometry geometry, Material material)
    {
        var mesh = new Mesh { name = label };
        mesh.SetVertices(geometry.vertices); mesh.SetTriangles(geometry.triangles,0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // These meshes have no colliders and are never edited after generation.
        mesh.UploadMeshData(true); owned.Add(mesh);
        var part = new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));
        part.transform.SetParent(root,false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        return renderer;
    }

    private void OnDestroy()
    {
        foreach (Object asset in owned) if (asset != null) Destroy(asset);
    }

    private sealed class Geometry
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();

        public void Ellipsoid(Vector3 center, Quaternion rotation, Vector3 radii)
        {
            const int sides = 10, rows = 6;
            int start = vertices.Count;
            for (int row = 0; row <= rows; row++)
            {
                float latitude = row*Mathf.PI/rows;
                for (int i = 0; i <= sides; i++)
                {
                    float a = i*Mathf.PI*2/sides;
                    vertices.Add(center + rotation*Vector3.Scale(radii,new Vector3(Mathf.Sin(latitude)*Mathf.Cos(a),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(a))));
                }
            }
            for (int row = 0; row < rows; row++) for (int i = 0; i < sides; i++)
            {
                int a = start+row*(sides+1)+i, b=a+sides+1;
                triangles.Add(a); triangles.Add(a+1); triangles.Add(b);
                triangles.Add(a+1); triangles.Add(b+1); triangles.Add(b);
            }
        }

        public void Tube(Vector3 center, Quaternion rotation, float outer, float inner, float height)
        {
            // Separate wall/end vertices keep the tube opening and flat rim legible.
            const int sides = 20;
            Vector2[] profile = {new Vector2(outer,-height/2),new Vector2(outer,height/2),new Vector2(inner,height/2),new Vector2(inner,-height/2),new Vector2(outer,-height/2)};
            for (int p=0;p<profile.Length-1;p++)
            {
                int start=vertices.Count;
                for (int ring=0;ring<2;ring++) for(int i=0;i<=sides;i++)
                {
                    float a=i*Mathf.PI*2/sides; Vector2 point=profile[p+ring];
                    vertices.Add(center+rotation*new Vector3(Mathf.Cos(a)*point.x,point.y,Mathf.Sin(a)*point.x));
                }
                for(int i=0;i<sides;i++)
                {
                    int a=start+i,b=a+sides+1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a+1);
                    triangles.Add(a+1); triangles.Add(b); triangles.Add(b+1);
                }
            }
        }
    }
}
