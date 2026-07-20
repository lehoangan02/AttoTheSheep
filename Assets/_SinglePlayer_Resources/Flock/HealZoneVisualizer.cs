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
    public Color waveColor = new Color(0.2f, 0.8f, 0.5f, 0.4f);

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
        transform.localScale = Vector3.one;
    }

    void Update()
    {

        if (flockManager == null || waveSprite == null || flockManager.activeLambs == null || flockManager.activeLambs.Count == 0)
            return;

        transform.position = flockManager.currentFlockCenter.Value;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        GameObject waveObj = new GameObject("RippleWave");
        waveObj.transform.position = transform.position;
        waveObj.transform.SetParent(transform);

        SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sprite = waveSprite;
        sr.color = waveColor;
        sr.sortingOrder = sortingOrder;

        float spriteUnitWidth = waveSprite.bounds.size.x;
        float normalizationFactor = (spriteUnitWidth > 0) ? (1f / spriteUnitWidth) : 1f;

        WaveBehavior waveAnim = waveObj.AddComponent<WaveBehavior>();

        waveAnim.targetScale = (flockManager.currentFlockRadius * 2f) * normalizationFactor * sizeMultiplier;
        waveAnim.duration = waveDuration;
    }
}

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
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float easeOutProgress = 1f - Mathf.Pow(1f - progress, 3f);
        float currentScale = Mathf.Lerp(0f, targetScale, easeOutProgress);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);

        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, progress);
        sr.color = c;
    }
}