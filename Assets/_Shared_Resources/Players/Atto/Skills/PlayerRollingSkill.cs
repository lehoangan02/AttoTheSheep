using UnityEngine;
using System.Collections;
using Unity.Netcode; // Bắt buộc cho NetworkEntity

// ĐỔI KẾ THỪA: Kế thừa BaseSkillComponent thay vì MonoBehaviour
public class PlayerRollingSkill : BaseSkillComponent
{
    [Header("Graphics References")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject dustVisual;

    [Header("Effects")]
    [SerializeField] private ParticleSystem dustParticle; 
    
    private bool isSkillActive = false;
    private float currentSpeedMultiplier = 1f;
    private RollingSkillData currentData;

    void Update()
    {
        // Hiệu ứng hình ảnh mượt mà phía Client (chỉ chạy khi isSkillActive = true)
        if (isSkillActive && dustVisual != null && dustVisual.activeSelf && currentData != null)
        {
            currentSpeedMultiplier = Mathf.MoveTowards(
                currentSpeedMultiplier, 
                currentData.maxSpeedMultiplier, 
                currentData.acceleration * Time.deltaTime
            );

            float dynamicRotationSpeed = currentData.baseRotationSpeed * currentSpeedMultiplier;
            dustVisual.transform.Rotate(0, 0, -dynamicRotationSpeed * Time.deltaTime);
        }
    }

    // =========================================================
    // 1. HÀM CHẠY TRÊN SERVER: TÍNH TOÁN VẬT LÝ VÀ TỐC ĐỘ
    // =========================================================
    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        // Ép kiểu Data an toàn
        if (data is RollingSkillData rollingData && caster != null)
        {
            // Tìm PlayerMovement để tăng tốc độ (Script PlayerMovement đã có sẵn hàm ApplyTemporarySpeedMultiplier)
            PlayerMovement movement = caster.GetComponent<PlayerMovement>();
            if (movement == null) movement = caster.GetComponentInChildren<PlayerMovement>();

            if (movement != null)
            {
                movement.ApplyTemporarySpeedMultiplier(rollingData.maxSpeedMultiplier, rollingData.duration, rollingData.AccelerationDuration);
                Debug.Log($"[SERVER LOG] Đã kích hoạt tăng tốc Lăn cho {caster.gameObject.name}");
            }
        }
    }

    // =========================================================
    // 2. HÀM CHẠY TRÊN CLIENT: HIỂN THỊ HÌNH ẢNH (BẬT CỤC BỤI)
    // =========================================================
    public override void ClientPlayVisual(SkillData data)
    {
        if (isSkillActive || data == null) return;
        
        if (data is RollingSkillData rollingData)
        {
            currentData = rollingData;
            StartCoroutine(RollSkillRoutine());
        }
    }

    // Coroutine xử lý hình ảnh (Giữ nguyên 100% logic cực đẹp của bạn)
    private IEnumerator RollSkillRoutine()
    {
        isSkillActive = true;
        currentSpeedMultiplier = 1f;

        if (normalVisual != null) normalVisual.SetActive(false);
        if (dustVisual != null) 
        {
            dustVisual.transform.localScale = Vector3.one; 
            dustVisual.SetActive(true);
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
                dustVisual.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);
            }
            yield return null;
        }

        if (dustVisual != null) dustVisual.SetActive(false);
        if (normalVisual != null) normalVisual.SetActive(true);

        isSkillActive = false;
        currentData = null; 
    }
}