using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;
    
    [Tooltip("Hotspot for Default Cursor. (0,0) is top-left.")]
    public Vector2 defaultHotSpot = new Vector2(22, 17);

    [Tooltip("Hotspot for Hover Cursor. (0,0) is top-left.")]
    public Vector2 hoverHotSpot = new Vector2(22, 17);

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private Vector2 GetHotSpot(Texture2D cursorTexture, bool isHoverCursor)
    {
        if (cursorTexture == null) return Vector2.zero;
        return isHoverCursor ? hoverHotSpot : defaultHotSpot;
    }

    private static Texture2D currentCursor = null;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;

        CancelInvoke(nameof(RevertCursor));
        
        if (hoverCursor != null && currentCursor != hoverCursor)
        {
            Cursor.SetCursor(hoverCursor, GetHotSpot(hoverCursor, true), CursorMode.Auto);
            currentCursor = hoverCursor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Add a tiny delay before reverting. This prevents flickering if the button 
        // is playing a bounce/scale animation that causes the hitbox boundary to rapidly 
        // cross back and forth over the stationary mouse pointer.
        Invoke(nameof(RevertCursor), 0.1f);
    }
    
    private void RevertCursor()
    {
        if (defaultCursor != null)
        {
            if (currentCursor != defaultCursor)
            {
                Cursor.SetCursor(defaultCursor, GetHotSpot(defaultCursor, false), CursorMode.Auto);
                currentCursor = defaultCursor;
            }
        }
        else
        {
            if (currentCursor != null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                currentCursor = null;
            }
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(RevertCursor));
        RevertCursor();
    }
}
