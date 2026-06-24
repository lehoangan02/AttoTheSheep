using UnityEngine;

public class HealWaveVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo FlockManager vào đây")]
    public FlockManager flockManager;
    
    [Tooltip("Kéo ảnh vòng tròn (Knob hoặc ảnh vành khuyên) vào đây")]
    public Sprite waveSprite;

    [Header("Wave Settings")]
    [Tooltip("Màu của gợn sóng")]
    public Color waveColor = new Color(0.2f, 0.8f, 0.5f, 0.4f); // Xanh ngọc mờ
    
    [Tooltip("Thời gian tồn tại của 1 gợn sóng (giây)")]
    public float waveDuration = 2f;
    
    [Tooltip("Bao lâu thì đẻ ra 1 sóng mới (giây). Càng nhỏ sóng càng dày.")]
    public float spawnInterval = 0.6f;
    
    [Tooltip("Lớp hiển thị (số âm để nằm dưới bầy cừu)")]
    public int sortingOrder = -5;

    [Header("Size Tuning (Chỉnh kích thước)")]
    [Tooltip("Nếu sóng vẫn nhỏ hoặc muốn sóng lan rộng hơn viền, hãy tăng số này lên (Ví dụ: 1.2, 1.5...)")]
    [Range(0.5f, 3f)]
    public float sizeMultiplier = 1.0f;

    private float spawnTimer = 0f;

    void Start()
    {
        // Khóa scale của object cha này về chuẩn 1, 1, 1 để không làm lệch các sóng con
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        if (flockManager == null || waveSprite == null) return;

        // Luôn bám theo tâm bầy cừu
        transform.position = flockManager.currentFlockCenter.Value;

        // Đếm thời gian để sinh ra gợn sóng mới
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        // Tạo 1 Game Object mới chứa gợn sóng
        GameObject waveObj = new GameObject("RippleWave");
        waveObj.transform.position = transform.position;
        waveObj.transform.SetParent(transform); // Nhét vào trong object cha cho gọn

        // Gắn Sprite và set màu
        SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sprite = waveSprite;
        sr.color = waveColor;
        sr.sortingOrder = sortingOrder;

        // --- THUẬT TOÁN TỰ ĐỘNG CHUẨN HÓA KÍCH THƯỚC ẢNH ---
        // Lấy kích thước thật của Sprite trong không gian Unity (đơn vị Unit)
        float spriteUnitWidth = waveSprite.bounds.size.x;
        // Hệ số chuẩn hóa: Biến mọi bức ảnh (dù to hay nhỏ) về đúng chuẩn kích thước 1 ô vuông Unity
        float normalizationFactor = (spriteUnitWidth > 0) ? (1f / spriteUnitWidth) : 1f;

        // Gắn script phụ để điều khiển hiệu ứng lan tỏa
        WaveBehavior waveAnim = waveObj.AddComponent<WaveBehavior>();
        
        // Đường kính đích = Bán kính bầy cừu * 2 * Hệ số chuẩn hóa ảnh * Hệ số tinh chỉnh tay
        waveAnim.targetScale = (flockManager.currentFlockRadius * 2f) * normalizationFactor * sizeMultiplier; 
        waveAnim.duration = waveDuration;
    }
}

// ==========================================
// CLASS PHỤ: Xử lý hoạt ảnh của từng gợn sóng
// ==========================================
public class WaveBehavior : MonoBehaviour
{
    public float targetScale;
    public float duration;

    private SpriteRenderer sr;
    private Color startColor;
    private float timer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
        transform.localScale = Vector3.zero; // Bắt đầu lan ra từ tâm (scale = 0)
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress >= 1f)
        {
            Destroy(gameObject); // Sóng tan biến hết thì tự hủy cho nhẹ máy
            return;
        }

        // Hiệu ứng phình to: Phình nhanh lúc đầu, chậm dần về cuối (Ease Out)
        float easeOutProgress = 1f - Mathf.Pow(1f - progress, 3f); 
        float currentScale = Mathf.Lerp(0f, targetScale, easeOutProgress);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        // Hiệu ứng mờ dần: Alpha giảm từ từ về 0
        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, progress);
        sr.color = c;
    }
}