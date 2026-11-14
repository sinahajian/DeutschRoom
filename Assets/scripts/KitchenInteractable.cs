using UnityEngine;

public abstract class KitchenInteractable : MonoBehaviour
{
    [SerializeField] private string displayName = "Object";

    public string DisplayName => displayName;
    public abstract string GetAction(PlayerInteraction player);
    public abstract void Interact(PlayerInteraction player);
}
