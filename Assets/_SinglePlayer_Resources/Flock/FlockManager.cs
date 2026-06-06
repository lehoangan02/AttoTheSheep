using UnityEngine;
using System.Collections.Generic;
using System; // BẮT BUỘC PHẢI CÓ để dùng Action (Event)

public class FlockManager : MonoBehaviour
{
    [Header("Flock Settings")]
    [SerializeField] private GameObject lambPrefab;
    [SerializeField] private int initialLambCount = 5;
    [SerializeField] private float baseRadius = 1f;
    [SerializeField] private float radiusMultiplier = 0.5f; // Hệ số k giãn cách

    // --- THÊM VÀO: Cấu hình mốc cừu ---
    [Header("Mốc Unlock Skill (Đồng bộ với PlayerSynergy)")]
    [SerializeField] private int lambsForSkill1 = 3;
    [SerializeField] private int lambsForSkill2 = 6;
    [SerializeField] private int lambsForSkill3 = 10;
    // ----------------------------------

    // --- THÊM VÀO: Cấu hình Tự động sinh cừu ---
    [Header("Auto Spawn Settings")]
    [Tooltip("Bật/tắt chế độ bầy tự động sinh cừu con")]
    [SerializeField] private bool enableAutoSpawn = true;
    
    [Tooltip("Thời gian (giây) để đẻ thêm 1 con cừu")]
    [SerializeField] private float autoSpawnInterval = 10f;
    
    private float spawnTimer = 0f; // Biến đếm thời gian
    // ----------------------------------

    // --- THÊM VÀO: Cấu hình Debug ---
    [Header("Debug Settings")]
    [Tooltip("Bật/tắt vẽ vòng tròn bán kính bầy trong Scene View")]
    [SerializeField] private bool showDebugRadius = true;
    // ----------------------------------

    // Đổi thành public getter để PlayerSynergy có thể "nhìn" thấy dữ liệu
    public List<LambAI> activeLambs { get; private set; } = new List<LambAI>();
    public Vector2 currentFlockCenter { get; private set; } 
    public float currentFlockRadius { get; private set; }

    // Sự kiện phát loa thông báo mỗi khi Cấp độ bầy (Tier) thay đổi
    public event Action<int> OnFlockTierChanged;

    void Start()
    {
        currentFlockCenter = transform.position;
        
        // Cách tìm Player tốt hơn cho môi trường Mạng (Tránh tìm nhầm Atto của đối thủ)
        // Lưu ý: Nếu Spawn qua mạng, có thể phải chờ Atto xuất hiện mới tìm được.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.OnMapClicked += HandleMapClicked;
        }

        SpawnInitialFlock();
    }

    // --- THÊM VÀO: Bộ đếm thời gian để sinh cừu ---
    void Update()
    {
        if (enableAutoSpawn)
        {
            spawnTimer += Time.deltaTime; // Cộng dồn thời gian mỗi frame
            
            if (spawnTimer >= autoSpawnInterval)
            {
                spawnTimer = 0f; // Reset lại bộ đếm
                
                // Gọi hàm đẻ cừu ngay tại Tâm bầy hiện tại
                SpawnLamb(currentFlockCenter);
                
                Debug.Log($"🐑 [MANAGER] Bầy cừu vừa tự sinh thêm 1 con! Tổng số lượng hiện tại: {activeLambs.Count}");
            }
        }
    }
    // ----------------------------------

    private void SpawnInitialFlock()
    {
        for (int i = 0; i < initialLambCount; i++)
        {
            SpawnLamb(currentFlockCenter);
        }
        UpdateFlockRadius();
    }

    public void SpawnLamb(Vector2 position)
    {
        GameObject lambObj = Instantiate(lambPrefab, position, Quaternion.identity);
        LambAI lambAI = lambObj.GetComponent<LambAI>();
        
        if (lambAI != null)
        {
            lambAI.Initialize(this); // Cho cừu biết ai là sếp của nó
            activeLambs.Add(lambAI);
            UpdateFlockRadius();
            
            // Báo cáo Cấp bầy mới
            OnFlockTierChanged?.Invoke(GetFlockTier());
        }
    }

    // Hàm gọi khi cừu bị chạm địch và tự Disable
    public void RemoveLamb(LambAI lamb)
    {
        if (activeLambs.Contains(lamb))
        {
            activeLambs.Remove(lamb);
            UpdateFlockRadius();
            
            // Báo cáo Cấp bầy mới (Bị giảm đi)
            OnFlockTierChanged?.Invoke(GetFlockTier());
        }
    }

    private void UpdateFlockRadius()
    {
        // Công thức tính bán kính bầy đàn dựa trên số lượng cừu hiện tại
        currentFlockRadius = baseRadius + (radiusMultiplier * Mathf.Sqrt(activeLambs.Count));
        
        // Cập nhật lại vị trí cho bọn cừu để chúng giãn ra hoặc co lại
        CommandFlock(currentFlockCenter);
    }

    private void HandleMapClicked(Vector2 targetPos)
    {
        Debug.Log($"🐑 [MANAGER] Đã nhận được lệnh từ Player! Ra lệnh cho cừu chạy tới: {targetPos}");
        CommandFlock(targetPos);
    }

    private void CommandFlock(Vector2 targetPos)
    {
        currentFlockCenter = targetPos;

        foreach (var lamb in activeLambs)
        {
            if (lamb != null && lamb.gameObject.activeInHierarchy)
            {
                // Chỉ truyền Tâm bầy và Bán kính, để cừu tự tính toán đường đi riêng!
                lamb.SetFlockData(currentFlockCenter, currentFlockRadius);
            }
        }
    }

    // --- HÀM MỚI: TÍNH TOÁN CẤP ĐỘ BẦY (TIER) ---
    public int GetFlockTier()
    {
        // Bạn có thể tùy chỉnh các mốc số lượng cừu ở đây cho phù hợp với cân bằng game
        // (Đã thay thế số cứng bằng biến cấu hình)
        if (activeLambs.Count >= lambsForSkill3) return 3;
        if (activeLambs.Count >= lambsForSkill2) return 2;
        if (activeLambs.Count >= lambsForSkill1) return 1;
        
        return 0; // Chưa đủ bầy
    }

    // Vẽ vòng tròn bầy đàn trong Scene view để bạn dễ Debug
    private void OnDrawGizmos()
    {
        // --- THÊM VÀO: Bọc bằng if để check điều kiện ---
        if (showDebugRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentFlockCenter, currentFlockRadius);
        }
        // ----------------------------------
    }
    
    // Thêm hàm này vào FlockManager
    public bool IsPositionInsideFlock(Vector2 targetPosition)
    {
        // Manager tự lấy tâm bầy và bán kính hiện tại (đã được cập nhật sẵn) để đo
        float dist = Vector2.Distance(targetPosition, currentFlockCenter);
        return dist <= currentFlockRadius;
    }
}