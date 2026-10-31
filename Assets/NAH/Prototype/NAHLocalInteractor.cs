using UnityEngine;
using UnityEngine.InputSystem;

// LOCAL PROTOTYPE ONLY. Server validation is required for multiplayer pickups.
public interface INAHInteractable
{
    void Interact(GameObject interactor);
}

[RequireComponent(typeof(PlayerInput))]
public sealed class NAHLocalInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;
    private InputAction interactAction;

    private void Awake()
    {
        interactAction = GetComponent<PlayerInput>().actions.FindAction("Interact", true);
        if (playerCamera == null)
        {
            Debug.LogError("Assign playerCamera to NAHLocalInteractor.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!interactAction.WasPressedThisFrame()) return;
        if (!Physics.Raycast(playerCamera.transform.position,
            playerCamera.transform.forward, out RaycastHit hit,
            interactionDistance, interactionMask, QueryTriggerInteraction.Ignore)) return;

        // Interface lookup at interaction time, not every rendered frame.
        var scripts = hit.collider.GetComponentsInParent<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script is INAHInteractable interactable)
            {
                interactable.Interact(gameObject);
                break;
            }
        }
    }
}
