using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class VisualPolishValidation
{
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    public static void Run()
    {
        Renderer floor = GameObject.Find("Floor").GetComponent<Renderer>();
        Check(floor.sharedMaterial.name == "OakFloor" && floor.sharedMaterial.GetTexture("_BaseMap") != null && floor.sharedMaterial.GetTexture("_BumpMap") != null,
            "Floor is missing the textured surface or normal map.");
        Check(!GameObject.Find("Plane").GetComponent<Renderer>().enabled, "Duplicate coplanar kitchen ground is visible.");
        foreach (string name in new[] { "Teller_Plate", "Schuessel_Bowl", "Glas_DrinkingGlass", "Contents_WineGlass" })
        {
            GameObject item = GameObject.Find(name);
            Transform visual = item.transform.Find("RefinedVisual");
            Check(visual != null && visual.GetComponent<Renderer>().enabled, "Refined tableware missing: " + name);
            Mesh mesh = visual.GetComponent<MeshFilter>().sharedMesh;
            Check(mesh.vertexCount > 400 && mesh.uv.Length == mesh.vertexCount && mesh.normals.Length == mesh.vertexCount, "Incomplete tableware geometry: " + name);
            Check(item.GetComponentsInChildren<Collider>().Length > 0, "Visual upgrade removed pickup collision: " + name);
        }
        Transform details = GameObject.Find("VisualPolishDetails")?.transform;
        Check(details != null && details.childCount > 10 && details.GetComponentsInChildren<Collider>().Length == 0, "Architectural trim blocks interaction or is missing.");
        UniversalRenderPipelineAsset mobile = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Check(Mathf.Approximately(mobile.renderScale, 1) && mobile.msaaSampleCount == 2, "Mobile rendering remains undersampled or lacks edge antialiasing.");
        foreach (MeshFilter filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/Art/VisualPolish/")) continue;
            Check(mesh.vertexCount > 0 && mesh.uv.Length == mesh.vertexCount && mesh.tangents.Length == mesh.vertexCount, "Polished mesh missing UVs/tangents: " + mesh.name);
            foreach (Vector3 point in mesh.vertices)
                Check(!float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z), "Invalid polished vertex.");
        }
        Debug.Log("VISUAL_POLISH_VALIDATION_PASS: materials/normal maps, refined tableware, preserved colliders, nonblocking trim, valid UV geometry, native mobile resolution and 2x MSAA.");
    }
}
