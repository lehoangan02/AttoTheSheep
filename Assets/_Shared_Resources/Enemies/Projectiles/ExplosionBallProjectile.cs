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

        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayers);
        foreach (Collider2D hit in hits)
        {
            NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
            if (target == null || target == source || !target.IsAlive) continue;
            target.TakeDamage(damage, source);

            if (onHitEffects != null && onHitEffects.Length > 0)
            {
                StatusEffectController effectController = target.GetComponent<StatusEffectController>();

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
