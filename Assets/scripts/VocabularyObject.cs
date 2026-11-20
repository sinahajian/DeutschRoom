using UnityEngine;

// Furniture and fixed plumbing can be identified without being picked up.
public sealed class VocabularyObject : KitchenInteractable
{
    public override string GetAction(PlayerInteraction player) => "Anhören";
    public override void Interact(PlayerInteraction player) => player.Identify(this);
}
