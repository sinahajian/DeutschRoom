using UnityEngine;

public sealed class Faucet : KitchenInteractable
{
    [SerializeField] private GameObject waterStream;
    [SerializeField] private GameObject splash;
    [SerializeField] private Transform lever;

    private Quaternion closedLeverRotation;
    private Vector3 streamScale;

    public bool IsRunning { get; private set; }
    public GameObject WaterStream => waterStream;

    private void Awake()
    {
        if (lever != null)
            closedLeverRotation = lever.localRotation;
        if (waterStream != null)
            streamScale = waterStream.transform.localScale;
        SetRunning(false);
    }

    public override string GetAction(PlayerInteraction player) => IsRunning ? "Turn water off" : "Turn water on";

    public override void Interact(PlayerInteraction player)
    {
        SetRunning(!IsRunning);
    }

    private void SetRunning(bool value)
    {
        IsRunning = value;
        if (waterStream != null)
            waterStream.SetActive(value);
        if (splash != null)
            splash.SetActive(value);
        if (lever != null)
            lever.localRotation = closedLeverRotation * Quaternion.Euler(0f, 0f, value ? 30f : 0f);
    }

    private void Update()
    {
        if (!IsRunning || waterStream == null)
            return;
        float width = 1f + 0.07f * Mathf.Sin(Time.time * 23f);
        waterStream.transform.localScale = Vector3.Scale(streamScale, new Vector3(width, 1f, width));
    }
}
