using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public sealed class VisualPolishCamera : MonoBehaviour
{
    private void OnEnable()
    {
        var data = GetComponent<Camera>().GetUniversalAdditionalCameraData();
        data.antialiasing = Application.isMobilePlatform ? AntialiasingMode.None : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
    }
}
