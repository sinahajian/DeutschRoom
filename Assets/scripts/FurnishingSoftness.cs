using System.Collections.Generic;
using UnityEngine;

// One-time presentation pass for existing furnishings, including scenes built before this pass.
// Roots, vocabulary components, anchors and interaction owners stay intact.
public sealed class FurnishingSoftness : MonoBehaviour
{
    private readonly List<Mesh> ownedMeshes = new List<Mesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var root = GameObject.Find("LivingHallFurnishings");
        if (root != null && root.GetComponent<FurnishingSoftness>() == null)
            root.AddComponent<FurnishingSoftness>().Apply();
    }

    private void Apply()
    {
        var padding = new Dictionary<Vector3, Mesh>();
        foreach (var filter in FindObjectsByType<MeshFilter>())
        {
            bool textileRoot = false;
            for (var parent = filter.transform.parent; parent != null; parent = parent.parent)
                if (parent.name == "LivingHallFurnishings" || parent.name == "BedroomFurnishings")
                    textileRoot = true;
            if (!textileRoot) continue;
            Mesh mesh = null;
            switch (filter.name)
            {
                case "FoldedThrow": mesh = ThrowMesh(); break;
                case "SeatCushion": case "PaddedBack": case "PaddedArm":
                case "BenchPad": case "MattressBody":
                case "SoftCushion": case "PillowBody":
                    Vector3 size = filter.transform.localScale;
                    if (!padding.TryGetValue(size, out mesh))
                    {
                        mesh = PaddedBox(size);
                        // Keep the folded throw readable for its MeshCollider; padding uses boxes.
                        mesh.UploadMeshData(true);
                        padding.Add(size, mesh); ownedMeshes.Add(mesh);
                    }
                    break;
            }
            if (mesh == null) continue;
            if (filter.name == "FoldedThrow") ownedMeshes.Add(mesh);
            filter.sharedMesh = mesh;
            // Keep existing box colliders for seats; the blanket's picking surface follows its folds.
            var collider = filter.GetComponent<MeshCollider>();
            if (collider != null && filter.name == "FoldedThrow") { collider.sharedMesh = null; collider.sharedMesh = mesh; }
        }
    }

    public static Mesh PaddedBox(Vector3 size)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        var normals = new List<Vector3>();
        Vector3 half = size * .5f;
        float radius = Mathf.Min(.075f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .38f);
        Vector3 coreHalf = half - Vector3.one * radius;
        const int segments = 12;
        for (int axis = 0; axis < 3; axis++) foreach (int side in new[] { -1, 1 })
        {
            int a = (axis + 1) % 3, b = (axis + 2) % 3, start = vertices.Count;
            for (int j = 0; j <= segments; j++) for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments, v = j / (float)segments;
                Vector3 p = Vector3.zero; p[axis] = side * half[axis];
                p[a] = (u * 2 - 1) * half[a]; p[b] = (v * 2 - 1) * half[b];
                Vector3 core = new Vector3(Mathf.Clamp(p.x,-coreHalf.x,coreHalf.x), Mathf.Clamp(p.y,-coreHalf.y,coreHalf.y), Mathf.Clamp(p.z,-coreHalf.z,coreHalf.z));
                Vector3 normal = (p - core).normalized;
                p = core + normal * radius;
                // Broad, restrained loft makes upholstery read as stuffed fabric rather than timber.
                p[axis] += side * radius * .14f * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI);
                vertices.Add(new Vector3(p.x / size.x, p.y / size.y, p.z / size.z));
                normals.Add(Vector3.Scale(normal, size).normalized);
                uv.Add(new Vector2(u * size[a] * 3, v * size[b] * 3));
            }
            Grid(indices, start, segments, segments, side > 0, false);
        }
        var mesh = Finish("Padded textile", vertices, uv, indices);
        mesh.SetNormals(normals); mesh.RecalculateTangents();
        return mesh;
    }

    public static Mesh ThrowMesh()
    {
        // Coordinates relative to Living_Throw: over the back, onto the seat, down the front.
        var path = new[] { new Vector2(.47f,.52f), new Vector2(.51f,.36f),
            new Vector2(.32f,.23f), new Vector2(.015f,.16f), new Vector2(.005f,-.10f),
            new Vector2(-.025f,-.30f), new Vector2(-.15f,-.38f), new Vector2(-.28f,-.39f) };
        const int nx = 32, ny = 84;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for (int j = 0; j <= ny; j++) for (int i = 0; i <= nx; i++)
        {
            float u = i/(float)nx, v = j/(float)ny, t = v * (path.Length-1);
            int k = Mathf.Min((int)t, path.Length-2); float f = t-k;
            Vector2 a=path[Mathf.Max(0,k-1)], b=path[k], c=path[k+1], d=path[Mathf.Min(path.Length-1,k+2)];
            Vector2 p=.5f*((2*b)+(-a+c)*f+(2*a-5*b+4*c-d)*f*f+(-a+3*b-3*c+d)*f*f*f);
            float fold = Mathf.Sin(u*Mathf.PI*8 + v*.8f)*.009f + Mathf.Sin(u*Mathf.PI*17-v*2)*.003f;
            float edge = Mathf.Pow(Mathf.Abs(u-.5f)*2, 6);
            vertices.Add(new Vector3((u-.5f)*.60f + .018f*Mathf.Sin(v*4), p.x+fold-edge*.007f, p.y+fold*.35f));
            uv.Add(new Vector2(u*1.8f,v*4));
        }
        Grid(indices,0,nx,ny,true,false);
        return Finish("Soft draped sofa throw",vertices,uv,indices);
    }

    private static void Grid(List<int> indices,int start,int nx,int ny,bool forward,bool twoSided)
    {
        for(int j=0;j<ny;j++) for(int i=0;i<nx;i++)
        {
            int a=start+j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
            indices.Add(a); indices.Add(forward ? b : c); indices.Add(forward ? c : b);
            indices.Add(b); indices.Add(forward ? d : c); indices.Add(forward ? c : d);
            if(twoSided)
            {
                indices.Add(a); indices.Add(forward ? c : b); indices.Add(forward ? b : c);
                indices.Add(b); indices.Add(forward ? c : d); indices.Add(forward ? d : c);
            }
        }
    }
    private static Mesh Finish(string name,List<Vector3> vertices,List<Vector2> uv,List<int> indices)
    {
        var mesh=new Mesh{name=name}; mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
    private void OnDestroy() { foreach(var mesh in ownedMeshes) if(mesh != null) Destroy(mesh); }
}
