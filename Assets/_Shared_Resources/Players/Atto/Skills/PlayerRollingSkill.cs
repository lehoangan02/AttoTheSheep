using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

// ĐỔI KẾ THỪA: Kế thừa BaseSkillComponent thay vì MonoBehaviour
public class PlayerRollingSkill : BaseSkillComponent
{
    [Header("Graphics References")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject dustVisual;
    [SerializeField] private Transform spinningPart; // Chỉ xoay child này, không xoay cả dustVisual

    [Header("Effects")]
    [SerializeField] private ParticleSystem dustParticle; 
    
    [Header("Collision & Swallow")]
    [SerializeField] private Collider2D playerCollider; // Collider chính của Player (dùng để phát hiện va chạm khi lăn)
    [SerializeField] private LayerMask enemyLayer; // Layer của kẻ địch

    [Header("Scaling")]
    [SerializeField] private float maxScaleMultiplier = 1.5f; // Kích thước tối đa khi xù lông cuộn tròn

    private bool isSkillActive = false;
    private bool isVisualActive = false; // Flag riêng cho Client visual, không bị ảnh hưởng bởi Server
    private float currentSpeedMultiplier = 1f;
    private RollingSkillData currentData;

    // Danh sách kẻ địch đang bị nuốt (Server-side)
    private List<NetworkObject> swallowedEnemies = new List<NetworkObject>();
    private Coroutine damageCoroutine;

    void Update()
    {
        // Hiệu ứng hình ảnh mượt mà phía Client (chỉ chạy khi isVisualActive = true)
        if (isVisualActive && dustVisual != null && dustVisual.activeSelf && currentData != null)
        {
            currentSpeedMultiplier = Mathf.MoveTowards(
                currentSpeedMultiplier, 
                currentData.maxSpeedMultiplier, 
                currentData.acceleration * Time.deltaTime
            );

            float dynamicRotationSpeed = currentData.baseRotationSpeed * currentSpeedMultiplier;
            
            // Chỉ xoay spinningPart (child bên trong dustVisual), không xoay dustVisual gốc
            Transform target = spinningPart != null ? spinningPart : dustVisual.transform;
            target.Rotate(0, 0, -dynamicRotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ Server mới xử lý nuốt enemy
        if (!IsServer || !isSkillActive || currentData == null) return;

        // Kiểm tra enemy layer
        if (!LayerInMask(other.gameObject.layer, enemyLayer)) return;

        NetworkEntity enemyEntity = other.GetComponentInParent<NetworkEntity>();
        if (enemyEntity == null) return;

        NetworkObject enemyNetObj = enemyEntity.NetworkObject;
        if (enemyNetObj == null) return;

        // Không nuốt cùng 1 enemy 2 lần
        if (swallowedEnemies.Contains(enemyNetObj)) return;

        SwallowEnemy(enemyNetObj);
    }

    private bool LayerInMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    private void SwallowEnemy(NetworkObject enemyNetObj)
    {
        swallowedEnemies.Add(enemyNetObj);

        // Tắt collider enemy để không va chạm với player
        Collider2D enemyCollider = enemyNetObj.GetComponent<Collider2D>();
        if (enemyCollider != null) enemyCollider.enabled = false;

        // Tắt AI/movement của enemy
        LambAI lambAI = enemyNetObj.GetComponent<LambAI>();
        if (lambAI != null)
        {
            lambAI.enabled = false;
        }
        else
        {
            // Tắt Rigidbody nếu có
            Rigidbody2D enemyRb = enemyNetObj.GetComponent<Rigidbody2D>();
            if (enemyRb != null) enemyRb.linearVelocity = Vector2.zero;
        }

        // Ẩn enemy (visual) - nhưng vẫn giữ nguyên NetworkObject
        SetEnemyVisibility(enemyNetObj, false);

        // Nếu coroutine damage chưa chạy, bắt đầu
        if (damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(SwallowDamageRoutine());
        }

        Debug.Log($"[SERVER] Đã nuốt enemy {enemyNetObj.name} vào quả bóng!");
    }

    private void SetEnemyVisibility(NetworkObject enemyNetObj, bool visible)
    {
        // Ẩn/hiện tất cả Renderer trên enemy
        Renderer[] renderers = enemyNetObj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }

        // Nếu có SpriteRenderer trực tiếp trên object chính
        SpriteRenderer sprite = enemyNetObj.GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.enabled = visible;

        // Tắt Animator để enemy không chạy animation ngầm
        Animator anim = enemyNetObj.GetComponent<Animator>();
        if (anim != null) anim.enabled = visible;
    }

    private IEnumerator SwallowDamageRoutine()
    {
        // Gây 50 DMG/s cho mỗi kẻ địch bị nuốt
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (isSkillActive && swallowedEnemies.Count > 0)
        {
            yield return wait;

            // Duyệt ngược để có thể xóa an toàn
            for (int i = swallowedEnemies.Count - 1; i >= 0; i--)
            {
                NetworkObject enemyNetObj = swallowedEnemies[i];
                if (enemyNetObj == null || !enemyNetObj.IsSpawned)
                {
                    swallowedEnemies.RemoveAt(i);
                    continue;
                }

                NetworkEntity enemyEntity = enemyNetObj.GetComponent<NetworkEntity>();
                if (enemyEntity != null)
                {
                    // Gây sát thương chuẩn (không thể bị chặn bởi invulnerable của enemy)
                    enemyEntity.TakeDamage(Mathf.RoundToInt(currentData.damagePerSecond));
                }
            }
        }

        damageCoroutine = null;
    }

    // =========================================================
    // 1. HÀM CHẠY TRÊN SERVER: TÍNH TOÁN VẬT LÝ VÀ TỐC ĐỘ
    // =========================================================
    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        // Ép kiểu Data an toàn
        if (data is RollingSkillData rollingData && caster != null)
        {
            currentData = rollingData;

            // BẬT TRẠNG THÁI MIỄN NHIỄM
            caster.isInvulnerable.Value = true;

            // Tìm PlayerMovement để tăng tốc độ
            PlayerMovement movement = caster.GetComponent<PlayerMovement>();
            if (movement == null) movement = caster.GetComponentInChildren<PlayerMovement>();

            if (movement != null)
            {
                movement.ApplyTemporarySpeedMultiplier(rollingData.maxSpeedMultiplier, rollingData.duration, rollingData.AccelerationDuration);
                Debug.Log($"[SERVER LOG] Đã kích hoạt tăng tốc Lăn cho {caster.gameObject.name}");
            }

            // Kích hoạt collider trigger để nuốt enemy
            if (playerCollider != null)
            {
                playerCollider.isTrigger = true;
            }

            // Bắt đầu coroutine quản lý trạng thái Rolling trên Server
            StartCoroutine(ServerRollRoutine(rollingData, caster));
        }
    }

    private IEnumerator ServerRollRoutine(RollingSkillData rollingData, NetworkEntity caster)
    {
        isSkillActive = true;

        // Đợi thời gian tồn tại của skill
        yield return new WaitForSeconds(rollingData.duration);

        // Kết thúc: NHẢ TẤT CẢ KẺ ĐỊCH RA NGOÀI
        ReleaseAllSwallowedEnemies();

        // TẮT MIỄN NHIỄM
        caster.isInvulnerable.Value = false;

        // Tắt collider trigger
        if (playerCollider != null)
        {
            playerCollider.isTrigger = false;
        }

        // Ngừng damage coroutine
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        isSkillActive = false;
        // KHÔNG set currentData = null ở đây vì Client visual coroutine vẫn cần dùng
        // để chạy hiệu ứng biến mất dần (disappear animation) và bật lại normalVisual.
        // currentData sẽ được clear bởi Client's RollSkillRoutine() sau khi hoàn tất visual.
    }

    private void ReleaseAllSwallowedEnemies()
    {
        foreach (NetworkObject enemyNetObj in swallowedEnemies)
        {
            if (enemyNetObj == null || !enemyNetObj.IsSpawned) continue;

            // Bật lại collider
            Collider2D enemyCollider = enemyNetObj.GetComponent<Collider2D>();
            if (enemyCollider != null) enemyCollider.enabled = true;

            // Bật lại AI
            LambAI lambAI = enemyNetObj.GetComponent<LambAI>();
            if (lambAI != null)
            {
                lambAI.enabled = true;
            }

            // Hiện lại enemy
            SetEnemyVisibility(enemyNetObj, true);

            // Bật lại Animator
            Animator anim = enemyNetObj.GetComponent<Animator>();
            if (anim != null) anim.enabled = true;

            Debug.Log($"[SERVER] Đã nhả enemy {enemyNetObj.name} ra khỏi quả bóng!");
        }

        swallowedEnemies.Clear();
    }

    // =========================================================
    // 2. HÀM CHẠY TRÊN CLIENT: HIỂN THỊ HÌNH ẢNH (BẬT CỤC BỤI)
    // =========================================================
    public override void ClientPlayVisual(SkillData data)
    {
        // KHÔNG kiểm tra isSkillActive ở đây vì Server đã set nó = true trước khi ClientRpc được gọi
        // Chỉ kiểm tra data != null để tránh NullReference
        if (data == null) return;
        
        if (data is RollingSkillData rollingData)
        {
            currentData = rollingData;
            StartCoroutine(RollSkillRoutine());
        }
    }

    // Coroutine xử lý hình ảnh (visual scaling + rotation)
    private IEnumerator RollSkillRoutine()
    {
        isVisualActive = true;
        currentSpeedMultiplier = 1f;

        if (normalVisual != null) normalVisual.SetActive(false);
        if (dustVisual != null) 
        {
            dustVisual.transform.localScale = Vector3.one; 
            dustVisual.SetActive(true);

            // Phóng to quả bóng (xù lông + cuộn tròn)
            StartCoroutine(ScaleUpRoutine());
        }
        
        if (dustParticle != null) dustParticle.Play(); 

        // Chờ thời gian tồn tại chiêu
        yield return new WaitForSeconds(currentData.duration);
        
        if (dustParticle != null) dustParticle.Stop(); 

        // Hiệu ứng biến mất dần
        float elapsed = 0f;
        while (elapsed < currentData.disappearDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / currentData.disappearDuration;
            
            if (dustVisual != null)
            {
                dustVisual.transform.localScale = Vector3.Lerp(
                    dustVisual.transform.localScale, Vector3.zero, progress * 3f * Time.deltaTime
                );
            }
            yield return null;
        }

        if (dustVisual != null) dustVisual.SetActive(false);
        if (normalVisual != null) normalVisual.SetActive(true);

        isVisualActive = false;
        currentData = null; 
    }

    private IEnumerator ScaleUpRoutine()
    {
        if (dustVisual == null) yield break;

        float duration = currentData.duration * 0.3f; // Mất 30% thời gian để phóng to
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * maxScaleMultiplier;

        while (elapsed < duration && dustVisual != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            dustVisual.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        if (dustVisual != null)
        {
            dustVisual.transform.localScale = targetScale;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Cleanup nếu bị despawn bất ngờ
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }

        // Nhả enemy nếu còn
        if (IsServer && swallowedEnemies.Count > 0)
        {
            ReleaseAllSwallowedEnemies();
        }

        // Tắt invulnerable
        if (IsServer && NetworkObject != null)
        {
            NetworkEntity entity = GetComponentInParent<NetworkEntity>();
            if (entity != null)
            {
                entity.isInvulnerable.Value = false;
            }
        }

        isSkillActive = false;
        currentData = null;
    }

    private void OnDisable()
    {
        // Dọn dẹp khi component bị disable
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }
}