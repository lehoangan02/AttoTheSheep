using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine; 

public class PlayerRollingSkill : BaseSkillComponent
{
    [Header("Visual References")]
    [SerializeField] private GameObject normalVisual; // Object chứa hình cừu bình thường
    [SerializeField] private GameObject dustVisual;   // Object chứa cục bụi/quả bóng
    [SerializeField] private Transform spinMesh;      // Mesh bên trong Dust Visual để xoay
    [SerializeField] private ParticleSystem rollingDust; // Hiệu ứng bụi cuốn theo khi lăn

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource impulseSource; // Component tạo rung chấn Cinemachine

    private PlayerController rollController;

    // Lớp lưu trữ trạng thái quái vật trong dạ dày
    private class SwallowedEnemy
    {
        public GameObject Obj;
        public EnemyBrain Brain;
        public EnemyMotor Motor;
        public NetworkEntity Entity; 
        public SpriteRenderer[] Renderers;
        public Collider2D[] Colliders;
        public Vector3 OriginalScale; // Lưu lại kích thước gốc để phục hồi khi nhả ra
    }

    private List<SwallowedEnemy> stomach = new List<SwallowedEnemy>();

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is RollingSkillData rollData && controller != null)
        {
            rollController = controller;
            StartCoroutine(RollingRoutine(rollData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        // 1. GỌI CODE CỦA CLASS CHA ĐỂ TỰ ĐỘNG PHÁT SFX VÀ LOG
        base.ClientPlayVisual(data);
    }

    private IEnumerator RollingRoutine(RollingSkillData data, PlayerController controller)
    {
        Debug.Log("🌀 [ROLL] Bắt đầu cuộn tròn! Đang lấy đà...");
        
        // --- 1. SETUP VISUAL & TRẠNG THÁI ---
        if (normalVisual != null) normalVisual.SetActive(false);
        if (dustVisual != null) dustVisual.SetActive(true);
        
        // Bật Particle bụi
        if (rollingDust != null) rollingDust.Play();

        PlayerMovement pMovement = controller.GetComponentInChildren<PlayerMovement>();
        if (pMovement == null) pMovement = controller.GetComponentInParent<PlayerMovement>();

        float elapsed = 0f;
        float currentMultiplier = 1f;

        // --- 2. VÒNG LẶP MECHANIC ---
        while (elapsed < data.duration)
        {
            float deltaTime = Time.fixedDeltaTime;

            // Lấy đà tăng tốc dần đều
            if (currentMultiplier < data.maxSpeedMultiplier)
            {
                currentMultiplier += data.acceleration * deltaTime;
                currentMultiplier = Mathf.Min(currentMultiplier, data.maxSpeedMultiplier);
            }

            // Xoay mesh cục bông dựa trên tốc độ hiện tại
            if (spinMesh != null)
            {
                float currentRotationSpeed = data.baseRotationSpeed * currentMultiplier;
                spinMesh.Rotate(0, 0, -currentRotationSpeed * deltaTime);
            }

            // MECHANIC: HÚT VÀ NUỐT ĐỊCH
            Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, data.pullRadius, data.enemyLayer);
            foreach (var hit in hits)
            {
                float distance = Vector2.Distance(controller.transform.position, hit.transform.position);

                if (distance <= data.hitRadius)
                {
                    SwallowEnemy(hit.gameObject); // Đủ gần -> Nuốt vào bụng
                }
                else
                {
                    // Còn ở xa -> Hút từ từ vào tâm
                    hit.transform.position = Vector3.MoveTowards(
                        hit.transform.position, 
                        controller.transform.position, 
                        data.pullSpeed * deltaTime
                    );
                }
            }

            // Đảm bảo quái vật đã nằm trong bụng sẽ luôn đi theo Player
            for (int i = stomach.Count - 1; i >= 0; i--)
            {
                SwallowedEnemy swallowed = stomach[i];
                if (swallowed.Obj == null) 
                {
                    stomach.RemoveAt(i);
                    continue;
                }
                swallowed.Obj.transform.position = controller.transform.position;
            }

            elapsed += deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- 3. KẾT THÚC SKILL VÀ TÍNH TOÁN SÁT THƯƠNG 1 LẦN ---
        Debug.Log("🛑 [ROLL] Kết thúc cuộn tròn, chuẩn bị nhả địch!");

        // LƯU Ý: Nếu muốn sát thương tổng bằng với tổng sát thương DoT cũ, bạn có thể nhân data.damage với data.duration.
        // Ở đây mặc định lấy thẳng lượng sát thương data.damage
        int burstDamage = Mathf.FloorToInt(data.damage); 

        for (int i = stomach.Count - 1; i >= 0; i--)
        {
            SwallowedEnemy swallowed = stomach[i];
                    
            if (swallowed.Obj == null) 
            {
                stomach.RemoveAt(i);
                continue;
            }

            if (swallowed.Entity != null)
            {
                // Gây sát thương một lần duy nhất
                swallowed.Entity.TakeDamage(burstDamage);
                        
                // Nếu quái chết trong bụng -> Tiêu hóa thành công (không nhả ra nữa)
                if (!swallowed.Entity.IsAlive)
                {
                    Debug.Log($"💀 [ROLL] {swallowed.Obj.name} đã bị tiêu hóa!");
                    stomach.RemoveAt(i);
                }
            }
        }

        // Tắt Particle bụi
        if (rollingDust != null) rollingDust.Stop();

        // Nhả những con địch còn sống ra xung quanh bằng Animation
        SpitOutEnemies(data);

        // GỌI RUNG CAMERA BẰNG CINEMACHINE TẠI ĐÂY
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(); 
        }

        // Trả lại Visual cừu bình thường
        if (dustVisual != null) dustVisual.SetActive(false);
        if (normalVisual != null) normalVisual.SetActive(true);
        if (spinMesh != null) spinMesh.localRotation = Quaternion.identity; 
    }

    private void SwallowEnemy(GameObject enemyObj)
    {
        if (stomach.Exists(e => e.Obj == enemyObj || e.Obj == enemyObj.transform.parent?.gameObject)) return;

        NetworkObject netObj = enemyObj.GetComponentInParent<NetworkObject>();
        if (netObj == null) netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        GameObject rootObj = netObj.gameObject;
        SwallowedEnemy swallowed = new SwallowedEnemy { Obj = rootObj };

        swallowed.Brain = rootObj.GetComponent<EnemyBrain>();
        swallowed.Motor = rootObj.GetComponent<EnemyMotor>();
        swallowed.Entity = rootObj.GetComponent<NetworkEntity>(); 
        swallowed.Renderers = rootObj.GetComponentsInChildren<SpriteRenderer>();
        swallowed.Colliders = rootObj.GetComponentsInChildren<Collider2D>();
        swallowed.OriginalScale = rootObj.transform.localScale; // Lưu lại scale gốc

        // Đóng băng AI và di chuyển ngay lập tức
        if (swallowed.Brain != null) swallowed.Brain.IsFrozen = true;
        if (swallowed.Motor != null) swallowed.Motor.IsFrozen = true;

        // Tắt vật lý (Collider & Rigidbody) để không cản đường
        foreach (var col in swallowed.Colliders) if (col != null) col.enabled = false;
        Rigidbody2D rb = rootObj.GetComponent<Rigidbody2D>();
        if (rb != null) 
        { 
            rb.linearVelocity = Vector2.zero; 
            rb.isKinematic = true; 
        }

        stomach.Add(swallowed);

        // Chạy animation hút tọt vào bụng
        StartCoroutine(VisualSuckIn(swallowed));
    }

    private IEnumerator VisualSuckIn(SwallowedEnemy enemy)
    {
        float duration = 0.2f; // Thời gian hút tọt vào (giây)
        float elapsed = 0f;
        Vector3 startPos = enemy.Obj.transform.position;

        while (elapsed < duration && enemy.Obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Kéo dần vào tâm người chơi
            enemy.Obj.transform.position = Vector3.Lerp(startPos, rollController.transform.position, t);
            // Thu nhỏ dần về 0
            enemy.Obj.transform.localScale = Vector3.Lerp(enemy.OriginalScale, Vector3.zero, t);
            
            yield return null;
        }

        // Sau khi hút xong -> Giấu hình ảnh hoàn toàn
        if (enemy.Obj != null)
        {
            foreach (var sr in enemy.Renderers) if (sr != null) sr.enabled = false;
            // Trả lại kích thước gốc để chuẩn bị cho lúc nhả ra
            enemy.Obj.transform.localScale = enemy.OriginalScale; 
        }
    }

    private void SpitOutEnemies(RollingSkillData data)
    {
        if (stomach.Count == 0) return;

        // Copy danh sách quái ra một list tạm để chạy hiệu ứng, 
        // đồng thời làm rỗng dạ dày (stomach) ngay lập tức
        List<SwallowedEnemy> enemiesToSpit = new List<SwallowedEnemy>(stomach);
        stomach.Clear();

        // Bắt đầu hiệu ứng bắn quái ra
        StartCoroutine(VisualPushOut(enemiesToSpit, data));
    }

    private IEnumerator VisualPushOut(List<SwallowedEnemy> enemiesToSpit, RollingSkillData data)
    {
        int enemyCount = enemiesToSpit.Count;
        float angleStep = 360f / enemyCount; 
        float startAngle = Random.Range(0f, 360f); 
        float baseRadius = data.pullRadius; // Dùng bán kính hút để làm bán kính đẩy ra

        Vector3[] targetPositions = new Vector3[enemyCount];
        Vector3 centerPos = rollController.transform.position;

        // --- 1. SETUP TRƯỚC KHI BẮN ---
        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = enemiesToSpit[i];
            if (enemy.Obj == null) continue;

            // Tính toán vị trí đích đến xung quanh vòng tròn
            float currentAngle = startAngle + (i * angleStep);
            Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            float randomRadius = baseRadius + Random.Range(-0.3f, 0.5f);
            targetPositions[i] = (Vector2)centerPos + (direction * randomRadius);

            // Đặt quái ở chính giữa bụng và hiện hình lên ngay
            enemy.Obj.transform.position = centerPos;
            if (enemy.Renderers != null) 
                foreach (var sr in enemy.Renderers) if (sr != null) sr.enabled = true;
        }

        // --- 2. CHẠY HOẠT ẢNH BẮN VĂNG RA ---
        float duration = 0.25f; // Thời gian bay ra ngoài (giây)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Công thức làm mượt: Bay nhanh ra lúc đầu, hãm phanh lúc sau (Ease Out Cubic)
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f); 

            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = enemiesToSpit[i];
                if (enemy.Obj != null)
                {
                    enemy.Obj.transform.position = Vector3.Lerp(centerPos, targetPositions[i], easeOutT);
                }
            }
            yield return null;
        }

        // --- 3. KẾT THÚC BẮN -> BẬT LẠI VẬT LÝ VÀ AI ---
        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = enemiesToSpit[i];
            if (enemy.Obj == null) continue;

            // Chốt hạ vị trí cuối cùng
            enemy.Obj.transform.position = targetPositions[i];

            // Reset trạng thái vật lý
            Rigidbody2D rb = enemy.Obj.GetComponent<Rigidbody2D>();
            if (rb != null) 
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector2.zero; 
            }

            // Rã đông AI và bật va chạm
            if (enemy.Brain != null) enemy.Brain.IsFrozen = false;
            if (enemy.Motor != null) enemy.Motor.IsFrozen = false;
            if (enemy.Colliders != null) 
                foreach (var col in enemy.Colliders) if (col != null) col.enabled = true;
        }
    }
}