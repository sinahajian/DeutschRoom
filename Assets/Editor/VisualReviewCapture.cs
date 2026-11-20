using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VisualReviewCapture
{
    public static void Before()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.delayCall += () => { try { Capture("before"); EditorApplication.Exit(0); } catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
    }
    public static void Capture(string suffix)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new System.InvalidOperationException("A graphics device is required for visual review.");
        Directory.CreateDirectory("Documentation/VisualReview");
        Camera camera = new GameObject("Visual review camera", typeof(Camera)).GetComponent<Camera>();
        camera.enabled = false; camera.fieldOfView = 65; camera.nearClipPlane = .025f; camera.farClipPlane = 100;
        camera.clearFlags = CameraClearFlags.Skybox; camera.allowHDR = true;
        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        RenderTexture target = new RenderTexture(1440, 1000, 24, RenderTextureFormat.ARGB32);
        target.Create();
        Vector3[] positions = { new Vector3(-1.8f, 1.6f, -.8f), new Vector3(1.4f, 1.25f, 2.3f), new Vector3(2.8f, 1.6f, -5.9f), new Vector3(.70f, 1.65f, -9.1f), new Vector3(4.15f,1.6f,-12.1f), new Vector3(6.15f,1.5f,-13.5f), new Vector3(1.05f,1.65f,-13.0f), new Vector3(4.3f,1.65f,-8.0f),new Vector3(-1.1f,1.6f,-13.2f),new Vector3(7.5f,1.6f,-8.4f),new Vector3(-3.6f,1.6f,-6.7f),new Vector3(2.75f,1.6f,-8.7f),new Vector3(2.25f,1.6f,-7.6f),new Vector3(7.1f,1.6f,-13.3f),new Vector3(5.6f,1.55f,-13.8f),new Vector3(6.5f,1.6f,-8.7f),new Vector3(2.9f,1.6f,-6.8f),new Vector3(2.9f,1.6f,-10.7f),new Vector3(-2.4f,1.65f,3.0f),new Vector3(2.7f,1.5f,3.2f),new Vector3(-2.8f,1.4f,-7.8f),new Vector3(2.8f,1.35f,-8.2f) };
        Vector3[] looks = { new Vector3(.2f, 1.25f, 4.4f), new Vector3(0, .82f, 1), new Vector3(2.8f, 1.4f, -14), new Vector3(-2.1f, 1.15f, -6.8f), new Vector3(7.4f,1.0f,-14.6f), new Vector3(6.15f,.95f,-11.5f), new Vector3(-2.3f,1.0f,-13.5f), new Vector3(7.1f,1.0f,-8.2f),new Vector3(-1.9f,1.05f,-11.0f),new Vector3(5.9f,1.0f,-5.7f),new Vector3(-1.5f,1.0f,-9.9f),new Vector3(3.25f,1.1f,-10.8f),new Vector3(3.45f,.8f,-5.9f),new Vector3(8.7f,.85f,-11.7f),new Vector3(6.7f,.65f,-15.5f),new Vector3(5.9f,.85f,-10.3f),new Vector3(1.97f,1.45f,-6.12f),new Vector3(2.1f,.65f,-11.75f),new Vector3(-2.05f,1.08f,4.46f),new Vector3(1.95f,1.02f,4.4f),new Vector3(-4.7f,.6f,-7.8f),new Vector3(1.97f,.85f,-8.55f) };
        string[] names = { "kitchen", "objects", "hallway", "living-room", "bathroom", "bathroom-vanity", "bedroom-1", "bedroom-2", "bedroom-1-storage", "bedroom-2-study", "living-room-tv", "hallway-storage", "hallway-entry", "bathroom-laundry", "bathroom-cleaning", "bedroom-2-drying-rack", "hallway-keys", "hallway-umbrella", "kitchen-final-appliances", "kitchen-final-tools", "living-room-radiator", "hallway-switch-socket" };
        for (int i = 0; i < names.Length; i++)
        {
            camera.fieldOfView = i == 4 ? 80 : 65;
            camera.transform.position = positions[i]; camera.transform.LookAt(looks[i]);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            Texture2D image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes("Documentation/VisualReview/" + names[i] + "-" + suffix + ".png", image.EncodeToPNG());
            RenderTexture.active = previous; Object.DestroyImmediate(image);
        }
        target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(camera.gameObject);
        Debug.Log("VISUAL_REVIEW_CAPTURE_PASS: " + suffix);
    }
}
