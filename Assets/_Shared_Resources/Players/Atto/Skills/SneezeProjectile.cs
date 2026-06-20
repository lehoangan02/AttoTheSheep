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

    private Vector2 startPosition;
    private bool hasTriggeredPuddle = false; // Ngăn chặn việc sinh ra nhiều vũng nước cùng lúc

    public void Initialize(Vector2 direction, SneezeSkillData data, int overrideDamage = 0, bool flipX = false)
    {
        skillData = data;
        speed = data.projectileSpeed;
        maxDistance = data.projectileMaxDistance;
        damage = overrideDamage > 0 ? overrideDamage : (int)data.damage;

        startPosition = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Xoay hướng đạn
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Lật trục để bóng đổ luôn đúng
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

    private void FixedUpdate()
    {
        if (!IsServer || hasTriggeredPuddle) return;

        // Nếu bay hết tầm tối đa -> Tạo vũng nước tại đây
        float distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            CreatePuddleAndDespawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasTriggeredPuddle) return;

        // Kiểm tra xem đạn có đụng trúng quái / tường không
        if ((skillData.hitLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Nếu muốn đạn vẫn gây sát thương khi đập thẳng vào mặt quái, mở khóa đoạn này:
        /*
        NetworkEntity enemyEntity = other.GetComponent<NetworkEntity>() ?? other.GetComponentInParent<NetworkEntity>();
        if (enemyEntity != null) {
            enemyEntity.TakeDamage(damage);
        }
        */

        // Đụng trúng mục tiêu -> Tạo vũng nước tại chân mục tiêu
        CreatePuddleAndDespawn();
    }

    private void CreatePuddleAndDespawn()
    {
        hasTriggeredPuddle = true;

        if (skillData.puddlePrefab != null)
        {
            // Sinh vũng nước ra
            GameObject puddleObj = Instantiate(skillData.puddlePrefab, transform.position, Quaternion.identity);
            
            // Đồng bộ qua mạng
            NetworkObject netObj = puddleObj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            // Khởi tạo thông số làm chậm
            SlowPuddle puddleScript = puddleObj.GetComponent<SlowPuddle>();
            if (puddleScript != null)
            {
                puddleScript.Initialize(skillData.slowMultiplier, skillData.puddleDuration);
            }
        }

        // Tiêu hủy viên đạn
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