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
    private bool hasTriggeredPuddle = false; // Ngăn chặn việc sinh ra nhiều vũng nước cùng lúc

    public void Initialize(Vector2 direction, SneezeSkillData data, int overrideDamage = 0, bool flipX = false, NetworkEntity source = null)
    {
        // QUAN TRỌNG: Reset lại trạng thái để tránh lỗi khi Object được tái sử dụng (Pooling)
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

        // --- BỘ LỌC AN TOÀN ---
        // Bỏ qua nếu đụng trúng chính Player (người cast), các tia đạn khác, hoặc Bầy cừu
        if (other.gameObject.CompareTag("Player") || 
            other.GetComponent<SneezeProjectile>() != null || 
            other.GetComponent<LambAI>() != null) 
        {
            return;
        }

        // Kiểm tra xem có đụng trúng quái / tường theo Layer Mask không
        if ((skillData.hitLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // --- 1. CƠ CHẾ GÂY SÁT THƯƠNG ---
        NetworkEntity enemyEntity = other.GetComponent<NetworkEntity>() ?? other.GetComponentInParent<NetworkEntity>();

        if (enemyEntity != null) 
        {
            enemyEntity.TakeDamage(damage); 
            Debug.Log($"💥 [SneezeProjectile] Đã gây {damage} sát thương cho {other.name}!");
        }

        // --- 2. TẠO VŨNG NƯỚC VÀ BIẾN MẤT ---
        CreatePuddleAndDespawn();
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
                puddleScript.Initialize(skillData.slowEffect, sourceEntity, skillData.puddleDuration);
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