using UnityEngine;

[ExecuteAlways]
public class HitboxCounterScale : MonoBehaviour
{
    private RectTransform rt;

    void OnEnable()
    {
        rt = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (rt == null || transform.parent == null) return;

        Vector3 pScale = transform.parent.localScale;
        if (pScale.x != 0 && pScale.y != 0 && pScale.z != 0)
        {
            // Counter the parent's local scale so the Hitbox remains at a constant global scale.
            // This prevents the Hitbox boundary from expanding/shrinking during hover animations,
            // which completely stops the infinite OnPointerEnter/Exit flickering loop!
            rt.localScale = new Vector3(1f / pScale.x, 1f / pScale.y, 1f / pScale.z);
        }
    }
}
