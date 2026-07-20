using UnityEngine;
using UnityEngine.UI;

public class PlayerScreenUI : MonoBehaviour
{
    [Header("UI Size & Position")]
    [SerializeField] private Vector2 uiPosition = new Vector2(20f, -20f);
    [SerializeField] private Vector2 barSize = new Vector2(250f, 20f);
    [SerializeField] private float barSpacing = 10f;

    private NetworkEntity targetEntity;
    private RectTransform healthBarFill;
    private RectTransform manaBarFill;

    private int maxHP;
    private int maxMP;

    void Awake()
    {

        targetEntity = GetComponent<NetworkEntity>();
        if (targetEntity == null)
        {

            enabled = false;
            return;
        }
    }

    void Start()
    {

        System.Reflection.FieldInfo hpField = typeof(NetworkEntity).GetField("baseMaxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo mpField = typeof(NetworkEntity).GetField("baseMaxMana", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        maxHP = hpField != null ? (int)hpField.GetValue(targetEntity) : 100;
        maxMP = mpField != null ? (int)mpField.GetValue(targetEntity) : 100;

        CreateScreenUI();

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

        GameObject canvasGO = new GameObject("PlayerScreen_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject containerGO = new GameObject("StatusBars_Group");
        containerGO.transform.SetParent(canvasGO.transform);

        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = uiPosition;

        Sprite whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        healthBarFill = BuildScreenBar("HealthBar", containerRect, whiteSprite, Color.red, Vector2.zero);

        Vector2 manaOffset = new Vector2(0, -(barSize.y + barSpacing));
        manaBarFill = BuildScreenBar("ManaBar", containerRect, whiteSprite, Color.cyan, manaOffset);

        UpdateBarFill(healthBarFill, (float)targetEntity.currentHealth.Value / maxHP);
        UpdateBarFill(manaBarFill, (float)targetEntity.currentMana.Value / maxMP);
    }

    private RectTransform BuildScreenBar(string name, RectTransform parent, Sprite sprite, Color fillColor, Vector2 localPos)
    {

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
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        GameObject fillGO = new GameObject(name + "_Fill");
        fillGO.transform.SetParent(bgRect);

        RectTransform fillRect = fillGO.AddComponent<RectTransform>();

        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(barSize.x, 0);

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

        fillRect.sizeDelta = new Vector2(barSize.x * ratio, 0f);
    }
}