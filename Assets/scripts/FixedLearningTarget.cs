using UnityEngine;

[RequireComponent(typeof(VocabularyObject))]
public sealed class FixedLearningTarget : MonoBehaviour
{
    [SerializeField] private int order;
    public int Order => order;
    public KitchenInteractable Item => GetComponent<KitchenInteractable>();
}
