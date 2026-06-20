using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;
    
    [Tooltip("If true, automatically sets the click point to the center of the cursor image.")]
    public bool autoCenterHotSpot = true;
    
    [Tooltip("Manual hotspot if autoCenter is false. (0,0) is top-left.")]
    public Vector2 customHotSpot = Vector2.zero;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    private Vector2 GetHotSpot(Texture2D cursorTexture)
    {
        if (cursorTexture == null) return Vector2.zero;
        if (autoCenterHotSpot)
        {
            return new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
        }
        return customHotSpot;
    }

    private static Texture2D currentCursor = null;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.interactable) return;

        CancelInvoke(nameof(RevertCursor));
        
        if (hoverCursor != null && currentCursor != hoverCursor)
        {
            Cursor.SetCursor(hoverCursor, GetHotSpot(hoverCursor), CursorMode.Auto);
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
                Cursor.SetCursor(defaultCursor, GetHotSpot(defaultCursor), CursorMode.Auto);
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
