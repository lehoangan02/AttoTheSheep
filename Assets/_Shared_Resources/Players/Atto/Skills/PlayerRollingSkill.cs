using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine;// Cần thiết để gọi hệ thống rung của Cinemachine (Lưu ý: Nếu dùng Unity 6/Cinemachine 3, đổi thành 'using Unity.Cinemachine;')

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
        // Thực hiện VFX/Sound riêng ở Client nếu cần
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
        float damageAccumulator = 0f;
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
                
                // Mở comment dưới nếu PlayerMovement có biến nhận hệ số tốc độ
                // if (pMovement != null) pMovement.speedMultiplier = currentMultiplier;
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

            // MECHANIC: GÂY SÁT THƯƠNG THEO THỜI GIAN (DOT)
            float damageThisFrame = data.damage * deltaTime;
            damageAccumulator += damageThisFrame;

            if (damageAccumulator >= 1f)
            {
                int intDamage = Mathf.FloorToInt(damageAccumulator);
                damageAccumulator -= intDamage;

                for (int i = stomach.Count - 1; i >= 0; i--)
                {
                    SwallowedEnemy swallowed = stomach[i];
                    
                    if (swallowed.Obj == null) 
                    {
                        stomach.RemoveAt(i);
                        continue;
                    }

                    // Ép vị trí quái đi theo Player khi đang ở trong dạ dày
                    swallowed.Obj.transform.position = controller.transform.position;

                    if (swallowed.Entity != null)
                    {
                        swallowed.Entity.TakeDamage(intDamage);
                        
                        // Nếu quái chết trong bụng -> Tiêu hóa thành công
                        if (!swallowed.Entity.IsAlive)
                        {
                            Debug.Log($"💀 [ROLL] {swallowed.Obj.name} đã bị tiêu hóa!");
                            stomach.RemoveAt(i);
                        }
                    }
                }
            }

            elapsed += deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // --- 3. KẾT THÚC SKILL ---
        Debug.Log("🛑 [ROLL] Nhả địch ra và thắng phanh!");
        
        // Tắt Particle bụi
        if (rollingDust != null) rollingDust.Stop();

        // Nhả địch ra xung quanh
        SpitOutEnemies();

        // GỌI RUNG CAMERA BẰNG CINEMACHINE TẠI ĐÂY
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(); 
        }

        // Trả lại Visual cừu bình thường
        if (dustVisual != null) dustVisual.SetActive(false);
        if (normalVisual != null) normalVisual.SetActive(true);
        if (spinMesh != null) spinMesh.localRotation = Quaternion.identity; 
        
        // Trả lại hệ số tốc độ gốc nếu có dùng
        // if (pMovement != null) pMovement.speedMultiplier = 1f; 
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

        // Đóng băng AI và di chuyển của quái khi bị nuốt
        if (swallowed.Brain != null) swallowed.Brain.IsFrozen = true;
        if (swallowed.Motor != null) swallowed.Motor.IsFrozen = true;

        // Giấu hình ảnh và vật lý
        foreach (var sr in swallowed.Renderers) if (sr != null) sr.enabled = false;
        foreach (var col in swallowed.Colliders) if (col != null) col.enabled = false;

        stomach.Add(swallowed);
    }

    private void SpitOutEnemies()
    {
        foreach (var enemy in stomach)
        {
            if (enemy.Obj == null) continue;

            // Văng quái ra một vị trí ngẫu nhiên nhỏ quanh Player
            Vector2 randomOffset = Random.insideUnitCircle * 1.5f;
            enemy.Obj.transform.position = rollController.transform.position + (Vector3)randomOffset;

            // Rã đông quái (cho phép AI chạy lại)
            if (enemy.Brain != null) enemy.Brain.IsFrozen = false;
            if (enemy.Motor != null) enemy.Motor.IsFrozen = false;

            // Hiển thị lại hình ảnh và vật lý
            if (enemy.Renderers != null) 
                foreach (var sr in enemy.Renderers) if (sr != null) sr.enabled = true;
                
            if (enemy.Colliders != null) 
                foreach (var col in enemy.Colliders) if (col != null) col.enabled = true;
        }
        
        stomach.Clear();
    }
}