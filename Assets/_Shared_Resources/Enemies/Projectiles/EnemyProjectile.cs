using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    [SerializeField] protected float maxDistance = 15f;
    [SerializeField] protected LayerMask targetLayers = ~0;

    protected float speed = 8f;
    protected int damage = 100;
    protected StatusEffectData[] onHitEffects;
    protected Rigidbody2D rb;
    protected Vector2 direction;
    protected Vector3 startPosition;
    protected bool hasHit;
    protected NetworkEntity source;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Initialize(Vector2 dir, float spd, int dmg, StatusEffectData[] effects, NetworkEntity src)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        onHitEffects = effects;
        source = src;
        startPosition = transform.position;
        hasHit = false;
    }

    protected virtual void FixedUpdate()
    {
        if (!IsServer || hasHit) return;
        if (rb != null) rb.linearVelocity = direction * speed;
        if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            Despawn();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasHit) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        NetworkEntity target = other.GetComponentInParent<NetworkEntity>();
        if (target == null || target == source) return;
        if (!target.IsAlive) return;

        hasHit = true;
        target.TakeDamage(damage);

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

        Despawn();
    }

    protected void Despawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
