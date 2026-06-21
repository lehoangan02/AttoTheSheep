using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HitboxPadding : MonoBehaviour, ICanvasRaycastFilter
{
    [Tooltip("Amount of padding to REMOVE from the hitbox. E.g. Left=10 means the hitbox starts 10 units from the left edge.")]
    public float left;
    public float right;
    public float top;
    public float bottom;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (rectTransform == null) return true;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPoint);

        Rect rect = rectTransform.rect;
        
        // Shrink the rect by the padding values
        rect.xMin += left;
        rect.xMax -= right;
        rect.yMin += bottom;
        rect.yMax -= top;

        return rect.Contains(localPoint);
    }
}
