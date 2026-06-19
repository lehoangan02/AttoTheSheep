using UnityEngine;

/// <summary>
/// Pure-visual enemy health bar driven by two SpriteRenderers.
/// <para>
/// <c>baseBar</c> is the 9-slice border; <c>fillBar</c> is the actual HP fill.
/// Widths are expressed in world units. Each frame the bar counter-scales its X
/// so it always reads right-side-up in world space, even when the parent
/// (e.g. <c>EnemyMotor</c>) flips its facing direction.
/// </para>
/// </summary>
[DisallowMultipleComponent]
public class HealthBar : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("9-slice border sprite; spans the full max-HP width.")]
    [SerializeField] private SpriteRenderer baseBar;

    [Tooltip("HP fill sprite; width tracks current HP.")]
    [SerializeField] private SpriteRenderer fillBar;

    [Header("Sizing")]
    [Tooltip("Bar width (world units) per point of HP.")]
    [SerializeField] private float widthPerHP = 0.01f;

    [Tooltip("Bar height (world units). Used as Sliced size.y and as the Y reference when scaling Simple sprites.")]
    [SerializeField] private float barHeight = 0.2f;

    /// <summary>
    /// Updates both bars to reflect the given HP. <c>baseBar</c> is sized to
    /// <paramref name="maxHealth"/>; <c>fillBar</c> is sized to
    /// <paramref name="currentHealth"/>. No-op when <paramref name="maxHealth"/>
    /// is non-positive.
    /// </summary>
    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0) return;

        float baseWidth = maxHealth * widthPerHP;
        float fillWidth = Mathf.Max(0, currentHealth) * widthPerHP;

        SetBarWidth(baseBar, baseWidth);
        SetBarWidth(fillBar, fillWidth);
    }

    /// <summary>
    /// Applies a world-unit <paramref name="width"/> to <paramref name="sr"/>.
    /// <para>
    /// For <see cref="SpriteDrawMode.Sliced"/> sprites we drive
    /// <see cref="SpriteRenderer.size"/> (which preserves the 9-slice borders).
    /// For <see cref="SpriteDrawMode.Simple"/> sprites we fall back to scaling
    /// the transform's X against the sprite's bounds.
    /// </para>
    /// </summary>
    private void SetBarWidth(SpriteRenderer sr, float width)
    {
        if (sr == null) return;

        if (sr.drawMode == SpriteDrawMode.Sliced)
        {
            sr.size = new Vector2(width, barHeight);
        }
        else if (sr.sprite != null)
        {
            float spriteWidth = sr.sprite.bounds.size.x;
            if (spriteWidth <= 0f) return;

            Vector3 scale = sr.transform.localScale;
            scale.x = width / spriteWidth;
            sr.transform.localScale = scale;
        }
    }

    /// <summary>
    /// Counter-scales this transform's X so the bar always reads right-side-up
    /// in world space, even when the parent (e.g. <c>EnemyMotor</c>) flips its
    /// facing direction by negating its own <c>localScale.x</c>.
    /// </summary>
    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        // Mathf.Sign(0) returns 0; guard against a zeroed parent scale.
        float parentSign = Mathf.Sign(parent.localScale.x);
        if (parentSign == 0f) parentSign = 1f;

        Vector3 myScale = transform.localScale;
        myScale.x = Mathf.Abs(myScale.x) * parentSign;
        transform.localScale = myScale;
    }
}
