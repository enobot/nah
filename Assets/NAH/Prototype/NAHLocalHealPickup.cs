using UnityEngine;

// Attach to a visible object with a Collider and place it on interactionMask.
// LOCAL PROTOTYPE ONLY: server must authorize pickup and consumption later.
public sealed class NAHLocalHealPickup : MonoBehaviour, INAHInteractable
{
    [SerializeField, Min(0.1f)] private float healAmount = 20f;

    public void Interact(GameObject interactor)
    {
        var health = interactor.GetComponent<NAHLocalHealth>();
        if (health == null || health.IsDead || health.CurrentHealth >= health.MaxHealth)
            return;
        health.Heal(healAmount);
        gameObject.SetActive(false);
    }
}
