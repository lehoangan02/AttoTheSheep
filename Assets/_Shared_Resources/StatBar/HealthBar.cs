using UnityEngine;

[DisallowMultipleComponent]
public class HealthBar : MonoBehaviour
{
    [SerializeField] private RectTransform barFill;

    // Bar fill max width in pixels. This is the width of the bar when health is full.
    private float barFillMaxWidth;

    // Entity to track. If null, this component is a no-op.
    [SerializeField] private NetworkEntity entity;

    private void Awake()
    {
        if (entity == null)
        {
            entity = GetComponentInParent<NetworkEntity>();
        }
    }

    // Get max width of bar fill in pixels. This is the width of the bar when health is full.
    private void Start()
    {
        if (barFill != null)
        {
            barFillMaxWidth = barFill.rect.width;
        }

        if (entity != null && barFill != null)
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
        if (barFill == null || maxHealth <= 0) return;
        float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
        barFill.sizeDelta = new Vector2(barFillMaxWidth * ratio, barFill.sizeDelta.y);
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
