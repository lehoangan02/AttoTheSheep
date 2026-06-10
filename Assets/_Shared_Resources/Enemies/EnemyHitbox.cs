using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyEntity owner;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private bool activeOnEnable;
    [SerializeField] private bool deactivateAfterFirstHit;

    private readonly HashSet<NetworkEntity> hitTargets = new HashSet<NetworkEntity>();
    private Collider2D hitboxCollider;
    private bool isActive;

    protected EnemyEntity Owner => owner;

    protected virtual void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        hitboxCollider.isTrigger = true;

        if (owner == null)
        {
            owner = GetComponentInParent<EnemyEntity>();
        }

        SetActive(activeOnEnable);
    }

    public void Activate()
    {
        hitTargets.Clear();
        SetActive(true);
    }

    public void Deactivate()
    {
        SetActive(false);
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null || target == owner || target.currentHealth.Value <= 0) return;
        if (!hitTargets.Add(target)) return;

        ApplyHit(target);

        if (deactivateAfterFirstHit)
        {
            Deactivate();
        }
    }

    protected virtual void ApplyHit(NetworkEntity target)
    {
        if (owner == null) return;
        target.TakeDamage(owner.AttackDamage);
    }

    private void SetActive(bool active)
    {
        isActive = active;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = active;
        }
    }
}
