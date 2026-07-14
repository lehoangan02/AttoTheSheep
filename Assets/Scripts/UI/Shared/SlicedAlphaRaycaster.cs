using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SlicedAlphaRaycaster : MonoBehaviour, ICanvasRaycastFilter
{
    public float alphaThreshold = 0.5f;
    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (img == null || img.sprite == null || img.sprite.texture == null)
            return true;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(img.rectTransform, sp, eventCamera, out local))
            return false;

        Rect rect = img.rectTransform.rect;
        
        // Convert local point to (0,0) bottom-left
        local.x -= rect.xMin;
        local.y -= rect.yMin;

        // If not sliced, just do a simple normalized map
        if (img.type != Image.Type.Sliced || !img.hasBorder)
        {
            float u = local.x / rect.width;
            float v = local.y / rect.height;
            return CheckAlpha(u, v);
        }

        // 9-Slice mapping
        Vector4 b = img.sprite.border; // x=Left, y=Bottom, z=Right, w=Top
        
        // Rendered borders in UI units (approximate)
        float renderPPU = img.pixelsPerUnitMultiplier;
        if (renderPPU <= 0) renderPPU = 1;
        
        // In most standard setups, 1 pixel border = 1 local unit.
        // We will map the local point to the sprite texture.
        float texX = 0;
        float texY = 0;
        
        Rect texRect = img.sprite.textureRect;

        // X Axis Mapping
        if (local.x < b.x) // Left slice
        {
            texX = local.x;
        }
        else if (local.x > rect.width - b.z) // Right slice
        {
            texX = texRect.width - (rect.width - local.x);
        }
        else // Center slice (stretched)
        {
            float centerWidthUI = rect.width - b.x - b.z;
            float centerWidthTex = texRect.width - b.x - b.z;
            float t = (local.x - b.x) / centerWidthUI;
            texX = b.x + t * centerWidthTex;
        }

        // Y Axis Mapping
        if (local.y < b.y) // Bottom slice
        {
            texY = local.y;
        }
        else if (local.y > rect.height - b.w) // Top slice
        {
            texY = texRect.height - (rect.height - local.y);
        }
        else // Center slice (stretched)
        {
            float centerHeightUI = rect.height - b.y - b.w;
            float centerHeightTex = texRect.height - b.y - b.w;
            float t = (local.y - b.y) / centerHeightUI;
            texY = b.y + t * centerHeightTex;
        }

        float uFinal = texX / texRect.width;
        float vFinal = texY / texRect.height;

        return CheckAlpha(uFinal, vFinal);
    }

    private bool CheckAlpha(float u, float v)
    {
        try
        {
            Rect texRect = img.sprite.textureRect;
            Texture2D tex = img.sprite.texture;
            
            int px = Mathf.RoundToInt(texRect.x + u * texRect.width);
            int py = Mathf.RoundToInt(texRect.y + v * texRect.height);

            // Clamp to prevent out of bounds
            px = Mathf.Clamp(px, (int)texRect.xMin, (int)texRect.xMax - 1);
            py = Mathf.Clamp(py, (int)texRect.yMin, (int)texRect.yMax - 1);

            Color c = tex.GetPixel(px, py);
            return c.a >= alphaThreshold;
        }
        catch
        {
            return true; // Fallback to clickable if texture isn't readable
        }
    }
}
