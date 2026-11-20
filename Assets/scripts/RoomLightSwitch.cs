using UnityEngine;

// Separate action owner on the rocker; the parent remains a fixed learning target.
public sealed class RoomLightSwitch : KitchenInteractable
{
    [SerializeField] private Light[] lights;
    [SerializeField] private Transform rocker;
    public bool IsOn { get; private set; } = true;
    public override string GetAction(PlayerInteraction player) => IsOn ? "Ausschalten" : "Einschalten";
    public override void Interact(PlayerInteraction player)
    {
        IsOn=!IsOn;
        foreach(var light in lights)if(light!=null)light.enabled=IsOn;
        if(rocker!=null)rocker.localRotation=Quaternion.Euler(IsOn?-9:9,0,0);
    }
}
