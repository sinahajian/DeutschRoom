using UnityEngine;

// An action owner on an existing model part; identification metadata and fixture counts stay intact.
public sealed class LessonDevice : KitchenInteractable
{
    public enum DeviceMode { Toggle, Flush, Lid }
    private DeviceMode mode;
    private Light lamp;
    private GameObject effect;
    private Material effectMaterial;
    private Vector3 openPosition;
    private Quaternion openRotation;
    private float remaining, lidProgress;
    public bool IsOn { get; private set; }
    public bool IsBusy => remaining > 0 || (mode == DeviceMode.Lid && !Mathf.Approximately(lidProgress, IsOn ? 1 : 0));
    public uint InteractionSequence { get; private set; }
    public uint CompletedCycles { get; private set; }

    public void Configure(DeviceMode type, string name, Light light = null)
    {
        mode = type; lamp = light; SetDisplayName(name);
        openPosition = transform.localPosition; openRotation = transform.localRotation;
        SetLessonState(type == DeviceMode.Lid || (light != null && light.enabled));
    }
    public void AddFlow(Vector3 from, Vector3 to, bool hood = false)
    {
        effect = new GameObject("LessonDeviceEffect") { hideFlags = HideFlags.DontSave };
        effect.transform.SetParent(transform, true);
        var template = Resources.Load<Material>("Lessons/PlacementZone");
        effectMaterial = new Material(template) { hideFlags = HideFlags.DontSave };
        effectMaterial.color = hood ? new Color(.7f,.9f,.82f) : new Color(.25f,.72f,1);
        var line = effect.AddComponent<LineRenderer>(); line.sharedMaterial = effectMaterial;
        line.useWorldSpace = true; line.positionCount = 2; line.SetPositions(new[] { from, to });
        line.startWidth = hood ? .06f : .018f; line.endWidth = hood ? .12f : .025f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        effect.SetActive(IsOn);
    }
    public override string GetAction(PlayerInteraction player) => mode == DeviceMode.Flush ? (IsBusy ? "Spülung läuft" : "Spülen") :
        mode == DeviceMode.Lid ? (IsOn ? "Schließen" : "Öffnen") : (IsOn ? "Ausschalten" : "Einschalten");
    public override void Interact(PlayerInteraction player)
    {
        if (IsBusy) return;
        InteractionSequence = unchecked(InteractionSequence + 1);
        if (mode == DeviceMode.Flush) { remaining = 1.6f; IsOn = true; if (effect != null) effect.SetActive(true); }
        else { IsOn = !IsOn; if (lamp != null) lamp.enabled = IsOn; if (effect != null) effect.SetActive(IsOn); }
    }
    public void SetLessonState(bool value)
    {
        remaining = 0; IsOn = mode != DeviceMode.Flush && value;
        if (lamp != null) lamp.enabled = IsOn;
        if (effect != null) effect.SetActive(IsOn);
        if (mode == DeviceMode.Lid) { lidProgress = IsOn ? 1 : 0; ApplyLid(); }
    }
    private void Update()
    {
        if (mode == DeviceMode.Lid && IsBusy)
        { lidProgress = Mathf.MoveTowards(lidProgress, IsOn ? 1 : 0, Time.deltaTime / .3f); ApplyLid(); Physics.SyncTransforms(); }
        if (remaining <= 0) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0)
        { IsOn = false; CompletedCycles = unchecked(CompletedCycles + 1); if (effect != null) effect.SetActive(false); }
    }
    private void ApplyLid()
    {
        transform.localPosition = Vector3.Lerp(new Vector3(0,.515f,0), openPosition, lidProgress);
        transform.localRotation = Quaternion.Slerp(Quaternion.Euler(90,0,0), openRotation, lidProgress);
    }
    private void OnDestroy() { if (effectMaterial != null) Destroy(effectMaterial); }
}
