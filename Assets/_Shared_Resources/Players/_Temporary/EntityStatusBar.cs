using UnityEngine;
using UnityEngine.UI;

public class PlayerScreenUI : MonoBehaviour
{
    [Header("UI Size & Position")]
    [SerializeField] private Vector2 uiPosition = new Vector2(20f, -20f); // Tọa độ góc trên bên trái màn hình
    [SerializeField] private Vector2 barSize = new Vector2(250f, 20f);   // Độ dài và độ dày của mỗi thanh
    [SerializeField] private float barSpacing = 10f;                     // Khoảng cách giữa thanh máu và mana

    private NetworkEntity targetEntity;
    private RectTransform healthBarFill;
    private RectTransform manaBarFill;

    private int maxHP;
    private int maxMP;

    void Awake()
    {
        // Tự động tìm NetworkEntity gắn chung trên Object này
        targetEntity = GetComponent<NetworkEntity>();
        if (targetEntity == null)
        {
            Debug.LogError($"[PlayerScreenUI] Không tìm thấy NetworkEntity trên {gameObject.name}!");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // Bóc tách chỉ số Max từ lớp cha bằng Reflection (tránh lỗi do thuộc tính đang để protected)
        System.Reflection.FieldInfo hpField = typeof(NetworkEntity).GetField("baseMaxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo mpField = typeof(NetworkEntity).GetField("baseMaxMana", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        maxHP = hpField != null ? (int)hpField.GetValue(targetEntity) : 100;
        maxMP = mpField != null ? (int)mpField.GetValue(targetEntity) : 100;

        // Tự động dựng Canvas UI thẳng từ code
        CreateScreenUI();

        // Đăng ký nhận sự kiện đồng bộ từ mạng
        targetEntity.currentHealth.OnValueChanged += OnHealthChanged;
        targetEntity.currentMana.OnValueChanged += OnManaChanged;
    }

    void OnDestroy()
    {
        if (targetEntity != null)
        {
            targetEntity.currentHealth.OnValueChanged -= OnHealthChanged;
            targetEntity.currentMana.OnValueChanged -= OnManaChanged;
        }
    }

    private void CreateScreenUI()
    {
        // 1. Tự động tạo một Canvas độc lập hiển thị trên màn hình
        GameObject canvasGO = new GameObject("PlayerScreen_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Hiện đè lên màn hình găm cố định
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Tạo một Cụm chứa (Container) ở góc trên bên trái
        GameObject containerGO = new GameObject("StatusBars_Group");
        containerGO.transform.SetParent(canvasGO.transform);
        
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1); // Neo vào góc Trên - Trái
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = uiPosition;

        // Tạo chất liệu ảnh màu trắng mặc định từ code
        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        // 3. Vẽ thanh Máu (Nằm trên)
        healthBarFill = BuildScreenBar("HealthBar", containerRect, whiteSprite, Color.red, Vector2.zero);

        // 4. Vẽ thanh Mana (Nằm dưới thanh máu một khoảng bằng độ dày + khoảng cách)
        Vector2 manaOffset = new Vector2(0, -(barSize.y + barSpacing));
        manaBarFill = BuildScreenBar("ManaBar", containerRect, whiteSprite, Color.cyan, manaOffset);

        // Cập nhật tỷ lệ ban đầu khi vào game
        UpdateBarFill(healthBarFill, (float)targetEntity.currentHealth.Value / maxHP);
        UpdateBarFill(manaBarFill, (float)targetEntity.currentMana.Value / maxMP);
    }

    private RectTransform BuildScreenBar(string name, RectTransform parent, Sprite sprite, Color fillColor, Vector2 localPos)
    {
        // Khởi tạo Nền Đen (Background)
        GameObject bgGO = new GameObject(name + "_BG");
        bgGO.transform.SetParent(parent);
        
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.sizeDelta = barSize;
        bgRect.anchoredPosition = localPos;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.sprite = sprite;
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f); // Màu đen mờ làm nền

        // Khởi tạo Thanh Chạy Chỉ Số (Fill) làm con của nền đen
        GameObject fillGO = new GameObject(name + "_Fill");
        fillGO.transform.SetParent(bgRect);

        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        // Neo hẳn sang lề trái để khi co giãn nó sẽ tụt từ phải sang trái giống game chuẩn
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(barSize.x, 0); // Chiều cao căng theo cha (stretch)

        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.color = fillColor;

        return fillRect;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (maxHP <= 0) return;
        UpdateBarFill(healthBarFill, Mathf.Clamp01((float)newValue / maxHP));
    }

    private void OnManaChanged(int previousValue, int newValue)
    {
        if (maxMP <= 0) return;
        UpdateBarFill(manaBarFill, Mathf.Clamp01((float)newValue / maxMP));
    }

    private void UpdateBarFill(RectTransform fillRect, float ratio)
    {
        if (fillRect == null) return;
        // Thay đổi trực tiếp chiều rộng (Width) của thanh dựa trên tỷ lệ % chỉ số còn lại
        fillRect.sizeDelta = new Vector2(barSize.x * ratio, 0f);
    }
}