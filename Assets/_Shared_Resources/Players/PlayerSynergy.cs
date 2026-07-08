using UnityEngine;
using Unity.Netcode;

public class PlayerFlockBuffs : NetworkBehaviour
{
    private FlockManager myFlock;
    private NetworkEntity entity; 
    private PlayerSkills skills;

    [SerializeField] private float buffCheckInterval = 1f; 
    [SerializeField] private int baseHealAmount = 5;

    void Awake()
    {
        entity = GetComponentInParent<NetworkEntity>();
        skills = GetComponent<PlayerSkills>(); 
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            myFlock = FindFirstObjectByType<FlockManager>();

            if (myFlock != null)
            {
                // Vẫn giữ chu kỳ kiểm tra buff (Mặc định 1 giây/lần)
                InvokeRepeating(nameof(CheckFlockBuffs), buffCheckInterval, buffCheckInterval);
            }
            else
            {
                // [CHẾ ĐỘ KHÔNG CÓ CỪU] Mở khóa toàn bộ skill bằng cách gán số lượng cừu ảo cực lớn (Ví dụ: 999 con)
                UpdateSkillLambsServerRpc(999);
            }
        }
    }

    private void CheckFlockBuffs()
    {
        if (myFlock == null) return;
        
        // Chỉ cần kiểm tra Heal Zone để hồi máu (Skill Zone đã được FlockManager tự động lo)
        Vector3 parentPos = transform.parent != null ? transform.parent.position : transform.position;
        bool isInsideHealZone = myFlock.IsPositionInsideHealZone(parentPos);
        
        // Gửi thông tin hồi máu lên Server
        UpdateBuffsServerRpc(myFlock.activeLambs.Count, isInsideHealZone, myFlock.HealScale, myFlock.ManaScale);
    }

    [ServerRpc]
    private void UpdateBuffsServerRpc(int clientFlockSize, bool isInsideHealZone, float healScale, float manaScale)
    {
        // LƯU Ý: Đã xóa phần đồng bộ isInsideFlock và unlockedSkillTier ở đây 
        // vì FlockManager.cs đã làm việc đó liên tục và chính xác trên Server rồi!

        // CHỈ XỬ LÝ HỒI MÁU VÀ NĂNG LƯỢNG
        if (isInsideHealZone && clientFlockSize > 0 && entity != null)
        {
            if (entity.currentHealth.Value < entity.BaseMaxHealth)
            {
                int healAmount = Mathf.RoundToInt(healScale * clientFlockSize);
                entity.Heal(Mathf.Max(1, healAmount));
            }

            if (entity.currentMana.Value < entity.BaseMaxMana)
            {
                int manaAmount = Mathf.RoundToInt(manaScale * clientFlockSize);
                entity.RestoreMana(Mathf.Max(1, manaAmount));
            }
        }
    }

    // Hàm hỗ trợ cho chế độ chơi không có bầy cừu
    [ServerRpc]
    private void UpdateSkillLambsServerRpc(int simulatedLambCount)
    {
        if (skills != null)
        {
            // Gán số lượng cừu khổng lồ để pass mọi điều kiện unlock
            skills.currentLambCount.Value = simulatedLambCount;
            skills.isInsideFlock.Value = true;
        }
    }
}