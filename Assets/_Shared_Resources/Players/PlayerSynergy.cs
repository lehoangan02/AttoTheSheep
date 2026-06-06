using UnityEngine;
using Unity.Netcode;

// Yêu cầu phải có 2 component này đi kèm trên cùng GameObject
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(NetworkHealth))]
public class PlayerSynergy : NetworkBehaviour
{
    private FlockManager myFlock;
    private NetworkHealth health;
    private PlayerSkills skills;

    [Header("Cài đặt Buff Bầy cừu")]
    [SerializeField] private float buffCheckInterval = 1f; // 1 giây kiểm tra buff 1 lần
    [SerializeField] private int baseHealAmount = 5;

    // --- THÊM VÀO: Cấu hình mốc cừu ---
    [Header("Mốc số lượng cừu để Unlock Skill")]
    [Tooltip("Số cừu cần để mở Skill 1")]
    [SerializeField] private int lambsForSkill1 = 3;
    
    [Tooltip("Số cừu cần để mở Skill 2")]
    [SerializeField] private int lambsForSkill2 = 6;
    
    [Tooltip("Số cừu cần để mở Skill 3 (Chiêu cuối)")]
    [SerializeField] private int lambsForSkill3 = 10;
    // ----------------------------------

    void Awake()
    {
        health = GetComponent<NetworkHealth>();
        skills = GetComponent<PlayerSkills>();
    }

    public override void OnNetworkSpawn()
    {
        // Chỉ máy của Chủ sở hữu Atto (Owner) mới phải làm việc tìm kiếm và đo khoảng cách
        if (IsOwner)
        {
            myFlock = FindFirstObjectByType<FlockManager>();
            InvokeRepeating(nameof(CheckFlockSynergy), buffCheckInterval, buffCheckInterval);
        }
    }

    // Hàm này chạy trên Client của người chơi mỗi 1 giây
    private void CheckFlockSynergy()
    {
        if (myFlock == null) return;

        int flockSize = myFlock.activeLambs.Count;
        
        // Chỉ cần "hỏi" FlockManager là xong!
        bool isInsideFlock = myFlock.IsPositionInsideFlock(transform.position);

        // Báo cáo dữ liệu bầy đàn của mình lên Server
        UpdateSynergyServerRpc(flockSize, isInsideFlock);
    }

    // Hàm này CHỈ CHẠY TRÊN SERVER để ra quyết định cuối cùng
    [ServerRpc]
    private void UpdateSynergyServerRpc(int clientFlockSize, bool isClientInsideFlock)
    {
        // 1. TÍNH TOÁN CẤP ĐỘ SKILL (Ví dụ: 3 cừu mở skill 1, 6 cừu mở skill 2, 10 cừu mở skill 3)
        int calculatedTier = 0;
        
        // (Đã thay thế số cứng bằng biến cấu hình)
        if (clientFlockSize >= lambsForSkill3) calculatedTier = 3;
        else if (clientFlockSize >= lambsForSkill2) calculatedTier = 2;
        else if (clientFlockSize >= lambsForSkill1) calculatedTier = 1;

        // Mở khóa Skill an toàn trên Server
        skills.unlockedSkillTier.Value = calculatedTier;

        // 2. TÍNH TOÁN HỒI MÁU (Chỉ hồi nếu Atto đang đứng trong vòng bầy cừu)
        if (isClientInsideFlock && clientFlockSize > 0)
        {
            int healAmount = baseHealAmount + (clientFlockSize * 1); // Cứ thêm 1 con cừu là hồi thêm 1 máu
            health.Heal(healAmount);
        }
    }
}