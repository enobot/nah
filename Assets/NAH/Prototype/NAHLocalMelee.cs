using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// LOCAL PROTOTYPE ONLY. Not suitable as network-authoritative damage code.
// Requires Input Action: Attack (Button), and a child Transform for attack origin.
[RequireComponent(typeof(PlayerInput))]
public sealed class NAHLocalMelee : MonoBehaviour
{
    [SerializeField] private Transform attackOrigin;
    [SerializeField, Min(0.1f)] private float reach = 1.5f;
    [SerializeField, Min(0.1f)] private float hitRadius = 0.75f;
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0.05f)] private float cooldown = 0.45f;
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask environmentOccluders;

    private readonly Collider[] results = new Collider[32];
    private readonly HashSet<NAHLocalHealth> alreadyHit = new HashSet<NAHLocalHealth>();
    private InputAction attackAction;
    private NAHLocalHealth ownHealth;
    private float nextAttackTime;

    private void Awake()
    {
        attackAction = GetComponent<PlayerInput>().actions.FindAction("Attack", true);
        ownHealth = GetComponent<NAHLocalHealth>();
        if (attackOrigin == null)
        {
            Debug.LogError("Assign an attackOrigin in NAHLocalMelee.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + cooldown;
            Attack();
        }
    }

    private void Attack()
    {
        alreadyHit.Clear();
        Vector3 center = attackOrigin.position + attackOrigin.forward * reach;
        int count = Physics.OverlapSphereNonAlloc(
            center, hitRadius, results, hittableLayers, QueryTriggerInteraction.Ignore);
        // Buffer saturation can hide valid targets: log and size it for your arena.
        if (count == results.Length)
            Debug.LogWarning("Melee collider buffer may be full", this);

        for (int i = 0; i < count; i++)
        {
            NAHLocalHealth target = results[i].GetComponentInParent<NAHLocalHealth>();
            if (target == null || target == ownHealth || target.IsDead || alreadyHit.Contains(target))
                continue;
            Vector3 toTarget = target.transform.position - attackOrigin.position;
            if (toTarget.sqrMagnitude < 0.0001f ||
                Vector3.Dot(attackOrigin.forward, toTarget.normalized) < 0.2f)
                continue;
            if (Physics.Linecast(attackOrigin.position, results[i].bounds.center,
                environmentOccluders, QueryTriggerInteraction.Ignore))
                continue;
            alreadyHit.Add(target);
            target.TakeDamage(damage);
        }
    }
}
