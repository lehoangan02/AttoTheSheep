using Unity.Netcode;
using UnityEngine;

public class ExplosionBallProjectile : EnemyProjectile
{
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private GameObject explosionVFXPrefab;

    protected override void FixedUpdate()
    {
        if (!IsServer || hasHit) return;
        if (rb != null) rb.linearVelocity = direction * speed;
        if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            Explode();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasHit) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;
        Explode();
    }

    private void Explode()
    {
        if (!IsServer || hasHit) return;
        hasHit = true;

        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
            NetworkObject vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null) vfxNetObj.Spawn();
        }
        else
        {
            Debug.LogWarning("[ExplosionBallProjectile] explosionVFXPrefab is null - no visual spawned.");
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayers);
        foreach (Collider2D hit in hits)
        {
            NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
            if (target == null || target == source || !target.IsAlive) continue;
            target.TakeDamage(damage, source);
            Debug.Log($"ExplosionBallProjectile hit {target.name} for {damage} damage.");

            if (onHitEffects != null && onHitEffects.Length > 0)
            {
                StatusEffectController effectController = target.GetComponent<StatusEffectController>();
                Debug.Log($"Applying {onHitEffects.Length} effects to {target.name}");
                if (effectController != null)
                {
                    foreach (StatusEffectData effect in onHitEffects)
                    {
                        if (effect == null) continue;
                        effectController.ApplyEffect(effect, 0f, source);
                    }
                }
            }
        }

        Despawn();
    }
}
