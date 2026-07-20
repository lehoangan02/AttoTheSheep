using UnityEngine;
using Unity.Netcode;

public class SneezeProjectile : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    private SneezeSkillData skillData;
    private float speed;
    private float maxDistance;
    private int damage;
    private NetworkEntity sourceEntity;

    private Vector2 startPosition;
    private bool hasTriggeredPuddle = false;

    public void Initialize(Vector2 direction, SneezeSkillData data, int overrideDamage = 0, bool flipX = false, NetworkEntity source = null)
    {

        hasTriggeredPuddle = false;
        sourceEntity = source;

        skillData = data;
        speed = data.projectileSpeed;
        maxDistance = data.projectileMaxDistance;
        damage = overrideDamage > 0 ? overrideDamage : (int)data.damage;

        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Vector3 localScale = transform.localScale;
        if (direction.x < 0)
        {
            localScale.y = -Mathf.Abs(localScale.y);
            localScale.x = Mathf.Abs(localScale.x);
        }
        else
        {
            localScale.y = Mathf.Abs(localScale.y);
            localScale.x = Mathf.Abs(localScale.x);
        }
        transform.localScale = localScale;

        if (IsServer)
        {
            InitializeVisualsClientRpc(direction, speed, flipX);
        }
    }

    [ClientRpc]
    private void InitializeVisualsClientRpc(Vector2 direction, float speed, bool flipX)
    {
        if (IsServer) return; // Server already sets this in Initialize

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Vector3 localScale = transform.localScale;
        if (direction.x < 0)
        {
            localScale.y = -Mathf.Abs(localScale.y);
            localScale.x = Mathf.Abs(localScale.x);
        }
        else
        {
            localScale.y = Mathf.Abs(localScale.y);
            localScale.x = Mathf.Abs(localScale.x);
        }
        transform.localScale = localScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasTriggeredPuddle) return;

        if (other.gameObject.CompareTag("Player") ||
            other.GetComponent<SneezeProjectile>() != null ||
            other.GetComponent<LambAI>() != null)
        {
            return;
        }

        if ((skillData.hitLayer.value & (1 << other.gameObject.layer)) == 0) return;

        NetworkEntity enemyEntity = other.GetComponent<NetworkEntity>() ?? other.GetComponentInParent<NetworkEntity>();

        if (enemyEntity != null)
        {
            enemyEntity.TakeDamage(damage);

        }

        CreatePuddleAndDespawn();
    }

    private void FixedUpdate()
    {
        if (!IsServer || hasTriggeredPuddle) return;

        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            CreatePuddleAndDespawn();
        }
    }

    private void CreatePuddleAndDespawn()
    {
        hasTriggeredPuddle = true;

        if (skillData.puddlePrefab != null)
        {

            GameObject puddleObj = Instantiate(skillData.puddlePrefab, transform.position, Quaternion.identity);

            NetworkObject netObj = puddleObj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            SlowPuddle puddleScript = puddleObj.GetComponent<SlowPuddle>();
            if (puddleScript != null)
            {
                puddleScript.Initialize(skillData.slowEffect, sourceEntity, skillData.puddleDuration);
            }
        }

        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        if (IsServer && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}