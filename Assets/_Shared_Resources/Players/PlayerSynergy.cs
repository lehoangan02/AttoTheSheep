using UnityEngine;
using Unity.Netcode;

public class PlayerSynergy : NetworkBehaviour
{
    private FlockManager myFlock;
    private NetworkEntity entity; 
    private PlayerSkills skills;

    [SerializeField] private float buffCheckInterval = 1f; 
    [SerializeField] private int baseHealAmount = 5;

    void Awake()
    {
        // Tìm lõi chỉ số để hồi máu
        entity = GetComponentInParent<NetworkEntity>();
        
        // Tìm bảng Kỹ năng nằm cùng Object để mở khóa chiêu
        skills = GetComponent<PlayerSkills>(); 
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            myFlock = FindFirstObjectByType<FlockManager>();

            if (myFlock != null)
            {
                // Bắt đầu vòng lặp kiểm tra bầy cừu (Mặc định 1 giây 1 lần)
                InvokeRepeating(nameof(CheckFlockSynergy), buffCheckInterval, buffCheckInterval);
            }
            else
            {
                // [CHẾ ĐỘ MULTIPLAYER] Nếu không có cừu trên map, tự động mở khóa full chiêu (Tier 10)
                UpdateSkillTierServerRpc(10);
            }
        }
    }

    private void CheckFlockSynergy()
    {
        if (myFlock == null) return;
        
        // Xem người chơi có đứng trong vùng nào của bầy cừu không
        Vector3 parentPos = transform.parent != null ? transform.parent.position : transform.position;
        bool isInsideHealZone = myFlock.IsPositionInsideHealZone(parentPos);
        bool isInsideSkillZone = myFlock.IsPositionInsideSkillZone(parentPos);
        
        // Gửi thông tin lên Server
        UpdateSynergyServerRpc(myFlock.activeLambs.Count, myFlock.GetFlockTier(), isInsideHealZone, isInsideSkillZone, myFlock.HealScale, myFlock.ManaScale);
    }

    [ServerRpc]
    private void UpdateSynergyServerRpc(int clientFlockSize, int flockTier, bool isInsideHealZone, bool isInsideSkillZone, float healScale, float manaScale)
    {
        if (skills == null) return;

        // 0. CẬP NHẬT trạng thái "đang ở trong vùng skill" cho PlayerSkills
        skills.isInsideFlock.Value = isInsideSkillZone;

        // 1. MỞ KHÓA CHIÊU THỨC
        skills.unlockedSkillTier.Value = flockTier;

        // 2. HỒI PHỤC: Chỉ hồi khi đứng trong vùng heal của đàn cừu
        if (isInsideHealZone && clientFlockSize > 0 && entity != null)
        {
            // Hồi máu nếu chưa đầy
            if (entity.currentHealth.Value < entity.BaseMaxHealth)
            {
                int healAmount = Mathf.RoundToInt(healScale * clientFlockSize);
                entity.Heal(Mathf.Max(1, healAmount));
            }

            // Hồi mana nếu chưa đầy
            if (entity.currentMana.Value < entity.BaseMaxMana)
            {
                int manaAmount = Mathf.RoundToInt(manaScale * clientFlockSize);
                entity.RestoreMana(Mathf.Max(1, manaAmount));
            }
        }
    }

    // Hàm phụ trợ cho chế độ không có cừu
    [ServerRpc]
    private void UpdateSkillTierServerRpc(int tier)
    {
        if (skills != null) skills.unlockedSkillTier.Value = tier;
    }
}