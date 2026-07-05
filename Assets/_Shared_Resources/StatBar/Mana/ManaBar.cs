using UnityEngine;

[DisallowMultipleComponent]
public class ManaBar : MonoBehaviour
{
    [SerializeField] private RectTransform barBounds;

    [SerializeField] private RectTransform barFill;

    [SerializeField] private bool scaleWidthWithMaxMana;

    [SerializeField] private float maxManaReference = 100f;

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
            entity.currentMana.OnValueChanged += OnManaChanged;
            UpdateFill(entity.currentMana.Value, entity.BaseMaxMana);
        }
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.currentMana.OnValueChanged -= OnManaChanged;
        }
    }

    private void OnManaChanged(int oldValue, int newValue)
    {
        if (entity == null) return;
        UpdateFill(newValue, entity.BaseMaxMana);
    }

    private void UpdateFill(int currentMana, int maxMana)
    {
        if (barFill == null || barBounds == null || maxMana <= 0) return;

        if (scaleWidthWithMaxMana && maxManaReference > 0f)
        {
            float scaleFactor = entity.BaseMaxMana / maxManaReference;
            barBounds.sizeDelta = new Vector2(baseBoundsWidth * scaleFactor, barBounds.sizeDelta.y);
        }

        float ratio = Mathf.Clamp01((float)currentMana / maxMana);
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
