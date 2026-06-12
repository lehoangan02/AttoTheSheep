using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Đạn nước mũi: bay thẳng về phía trước, chạm quái thì gây sát thương + choáng rồi biến mất.
/// </summary>
public class SneezeProjectile : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    [Header("Settings")]
    public float speed = 15f;
    public float maxDistance = 8f;
    public int damage = 150;
    public float stunDuration = 1.5f;
    public LayerMask enemyLayer;

    private Vector2 startPosition;
    private bool hasHit = false;

    public void Initialize(Vector2 direction, SneezeSkillData data, int overrideDamage = 0)
    {
        speed = data.projectileSpeed;
        maxDistance = data.projectileMaxDistance;
        damage = overrideDamage > 0 ? overrideDamage : (int)data.damage;
        stunDuration = data.stunDuration;
        enemyLayer = data.enemyLayer;

        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Xoay đạn theo hướng bay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void FixedUpdate()
    {
        if (!IsServer || hasHit) return;

        // Tự hủy nếu bay quá xa
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            DespawnProjectile();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasHit) return;

        // Kiểm tra layer bằng bitmask
        if ((enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Lấy NetworkEntity từ enemy
        NetworkEntity enemyEntity = other.GetComponent<NetworkEntity>();
        if (enemyEntity == null) enemyEntity = other.GetComponentInParent<NetworkEntity>();
        if (enemyEntity == null) return;

        hasHit = true;

        // Gây sát thương
        enemyEntity.TakeDamage(damage);
        Debug.Log($"💧 [SNEEZE_PROJECTILE] Trúng {other.gameObject.name}! Gây {damage} DMG.");

        // Choáng
        StunEnemy(other.gameObject);

        // Hủy đạn
        DespawnProjectile();
    }

    private void StunEnemy(GameObject enemyObj)
    {
        EnemyMovement enemyMovement = enemyObj.GetComponent<EnemyMovement>();
        if (enemyMovement == null) enemyMovement = enemyObj.GetComponentInParent<EnemyMovement>();

        EnemyAI ai = enemyObj.GetComponent<EnemyAI>();
        if (ai == null) ai = enemyObj.GetComponentInParent<EnemyAI>();

        Rigidbody2D enemyRb = enemyObj.GetComponent<Rigidbody2D>();
        if (enemyRb == null) enemyRb = enemyObj.GetComponentInParent<Rigidbody2D>();

        // Kiểm tra nếu đã stun rồi
        if (enemyMovement != null && !enemyMovement.enabled) return;
        if (ai != null && !ai.enabled) return;

        if (enemyMovement != null) enemyMovement.enabled = false;
        if (ai != null) ai.enabled = false;
        if (enemyRb != null) enemyRb.linearVelocity = Vector2.zero;

        StartCoroutine(RemoveStunAfterDelay(enemyObj, enemyMovement, ai, enemyRb, stunDuration));
    }

    private System.Collections.IEnumerator RemoveStunAfterDelay(
        GameObject enemyObj, EnemyMovement movement, EnemyAI ai, Rigidbody2D rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (enemyObj == null) yield break;

        if (movement != null) movement.enabled = true;
        if (ai != null) ai.enabled = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Vector2 dir = rb != null ? rb.linearVelocity.normalized : Vector2.right;
        Gizmos.DrawRay(transform.position, dir * maxDistance);
    }
}