using UnityEngine;

// Identification metadata only: keep the owner's pickup/open/faucet action intact.
public sealed class VocabularyPart : MonoBehaviour
{
    [SerializeField] private string displayName;
    public string DisplayName => displayName;
    public KitchenInteractable Owner => GetComponentInParent<KitchenInteractable>();
}
