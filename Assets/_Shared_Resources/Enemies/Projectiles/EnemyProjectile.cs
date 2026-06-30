using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    [SerializeField] protected float speed = 8f;
    [SerializeField] protected float maxDistance = 15f;
    [SerializeField] protected int damage = 100;
    [SerializeField] protected EffectData[] onHitEffectData;
    [SerializeField] protected LayerMask targetLayers = ~0;

    protected Rigidbody2D rb;
    protected Vector2 direction;
    protected Vector3 startPosition;
    protected bool hasHit;
    protected NetworkEntity source;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void Initialize(Vector2 dir, float spd, int dmg, EffectData[] effectDatas, NetworkEntity src)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        onHitEffectData = effectDatas;
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

        if (onHitEffectData != null && onHitEffectData.Length > 0)
        {
            StatusEffectController effectController = target.GetComponent<StatusEffectController>();
            if (effectController != null)
            {
                foreach (EffectData effectData in onHitEffectData)
                {
                    if (effectData?.effect == null) continue;
                    float duration = effectData.duration > 0 ? effectData.duration : effectData.effect.duration;
                    effectController.ApplyEffect(effectData.effect, duration, source, effectData.damagePerTick);
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
