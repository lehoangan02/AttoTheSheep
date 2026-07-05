using UnityEngine;

[DisallowMultipleComponent]
public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform barBounds;

    [SerializeField] private RectTransform barFill;

    [SerializeField] private bool scaleWidthWithMaxHealth;

    [SerializeField] private float maxHealthReference = 100f;

    [SerializeField] private NetworkEntity entity;

    private float baseBoundsWidth;

    private void Awake()
    {
        if (entity == null)
        {
            entity = GetComponentInParent<NetworkEntity>();
        }
    }

    private void Start()
    {
        if (barBounds != null)
        {
            baseBoundsWidth = barBounds.rect.width;
        }

        if (entity != null && barFill != null && barBounds != null)
        {
            entity.currentHealth.OnValueChanged += OnHealthChanged;
            UpdateFill(entity.currentHealth.Value, entity.BaseMaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (entity == null) return;
        UpdateFill(newValue, entity.BaseMaxHealth);
    }

    private void UpdateFill(int currentHealth, int maxHealth)
    {
        if (barFill == null || barBounds == null || maxHealth <= 0) return;

        if (scaleWidthWithMaxHealth && maxHealthReference > 0f)
        {
            float scaleFactor = entity.BaseMaxHealth / maxHealthReference;
            barBounds.sizeDelta = new Vector2(baseBoundsWidth * scaleFactor, barBounds.sizeDelta.y);
        }

        float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
        barFill.sizeDelta = new Vector2(barBounds.rect.width * ratio, barFill.sizeDelta.y);
    }

    private void LateUpdate()
    {
        if (transform.parent == null) return;
        float parentSign = Mathf.Sign(transform.parent.localScale.x);
        if (parentSign == 0f) parentSign = 1f;
        Vector3 myScale = transform.localScale;
        myScale.x = Mathf.Abs(myScale.x) * parentSign;
        transform.localScale = myScale;
    }
}
