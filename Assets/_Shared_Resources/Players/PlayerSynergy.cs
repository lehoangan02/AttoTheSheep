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
        
        // Xem người chơi có đứng trong vòng sáng của bầy cừu không
        Vector3 parentPos = transform.parent != null ? transform.parent.position : transform.position;
        bool isInsideFlock = myFlock.IsPositionInsideFlock(parentPos);
        
        // Gửi thông tin (Số lượng cừu, Cấp độ bầy, Có đứng trong bầy không) lên Server
        UpdateSynergyServerRpc(myFlock.activeLambs.Count, myFlock.GetFlockTier(), isInsideFlock);
    }

    [ServerRpc]
    private void UpdateSynergyServerRpc(int clientFlockSize, int flockTier, bool isClientInsideFlock)
    {
        // 1. MỞ KHÓA CHIÊU THỨC: Nạp Cấp độ bầy vào Bảng Kỹ Năng
        if (skills != null)
        {
            skills.unlockedSkillTier.Value = flockTier;
        }

        // 2. HỒI MÁU: Chỉ hồi khi đứng trong vòng của đàn cừu
        if (isClientInsideFlock && clientFlockSize > 0 && entity != null)
        {
            entity.Heal(baseHealAmount + clientFlockSize);
        }
    }

    // Hàm phụ trợ cho chế độ không có cừu
    [ServerRpc]
    private void UpdateSkillTierServerRpc(int tier)
    {
        if (skills != null) skills.unlockedSkillTier.Value = tier;
    }
}