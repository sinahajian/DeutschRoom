using UnityEngine;

public abstract class KitchenInteractable : MonoBehaviour
{
    [SerializeField] private string displayName = "Object";

    [SerializeField] private Transform identificationPoint;
    public Transform IdentificationPoint => identificationPoint;
    public string DisplayName => displayName;
    protected void SetDisplayName(string value) => displayName = value;
    public abstract string GetAction(PlayerInteraction player);
    public abstract void Interact(PlayerInteraction player);
}
