using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class AutoFitHitbox : EditorWindow
{
    [MenuItem("GameObject/UI/Auto-Fit Hitbox to Image Alpha", false, 0)]
    public static void FitHitbox()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Hãy chọn cục Hitbox trong Hierarchy trước!", "OK");
            return;
        }

        RectTransform hitboxRect = selected.GetComponent<RectTransform>();
        if (hitboxRect == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object được chọn không có RectTransform!", "OK");
            return;
        }

        // Lấy Image của Parent
        Image parentImage = selected.transform.parent != null ? selected.transform.parent.GetComponent<Image>() : null;
        if (parentImage == null || parentImage.sprite == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Hitbox phải là con của một Object có chứa Image Component và đã gắn Sprite!", "OK");
            return;
        }

        Sprite sprite = parentImage.sprite;
        Texture2D tex = sprite.texture;
        
        // Tạm thời bật Read/Write để đọc pixel
        string assetPath = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // Đọc vùng có hình ảnh (loại bỏ viền trong suốt)
        Rect texRect = sprite.textureRect;
        int xMin = (int)texRect.xMax;
        int xMax = (int)texRect.xMin;
        int yMin = (int)texRect.yMax;
        int yMax = (int)texRect.yMin;

        Color[] pixels = tex.GetPixels((int)texRect.x, (int)texRect.y, (int)texRect.width, (int)texRect.height);
        int width = (int)texRect.width;
        int height = (int)texRect.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Chỉ lấy những pixel có độ đục (alpha) lớn hơn 5%
                if (pixels[y * width + x].a > 0.05f)
                {
                    if (x < xMin) xMin = x;
                    if (x > xMax) xMax = x;
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }
            }
        }

        if (xMin > xMax) 
        {
            EditorUtility.DisplayDialog("Lỗi", "Tấm ảnh này hoàn toàn trong suốt!", "OK");
            return;
        }

        // Tính toán độ dày viền trong suốt (padding) theo pixel
        float padLeft = xMin;
        float padRight = width - 1 - xMax;
        float padBottom = yMin; // Trong Unity, Y=0 nằm ở dưới cùng
        float padTop = height - 1 - yMax;

        float ppuMultiplier = parentImage.pixelsPerUnitMultiplier;
        if (ppuMultiplier <= 0) ppuMultiplier = 1f;

        // Ép Hitbox giãn đều theo Parent
        Undo.RecordObject(hitboxRect, "Auto Fit Hitbox");
        hitboxRect.anchorMin = Vector2.zero;
        hitboxRect.anchorMax = Vector2.one;

        // Áp dụng Padding vào Left, Bottom, Right, Top
        hitboxRect.offsetMin = new Vector2(padLeft / ppuMultiplier, padBottom / ppuMultiplier); // Left, Bottom
        hitboxRect.offsetMax = new Vector2(-padRight / ppuMultiplier, -padTop / ppuMultiplier); // Right, Top

        // Trả lại cài đặt ban đầu cho ảnh
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        EditorUtility.SetDirty(selected);
        Debug.Log($"[Auto-Fit] Đã cắt bỏ viền trong suốt: Trái {padLeft}px, Phải {padRight}px, Trên {padTop}px, Dưới {padBottom}px.");
    }
}
