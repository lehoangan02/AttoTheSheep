using UnityEngine;
using UnityEditor;
using System.IO;

public class BlurTool : EditorWindow
{
    [MenuItem("Tools/Fix Cursor Warnings")]
    public static void FixCursorWarnings()
    {
        string folderPath = "Assets/Tiny Swords/UI Elements/Cursors";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                
                // Unity requires: RGBA32, readable, alphaIsTransparency, no mip chain
                if (!importer.isReadable) { importer.isReadable = true; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                
                // Format RGBA32
                var settings = importer.GetDefaultPlatformTextureSettings();
                if (settings.format != TextureImporterFormat.RGBA32)
                {
                    settings.format = TextureImporterFormat.RGBA32;
                    importer.SetPlatformTextureSettings(settings);
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    fixedCount++;
                }
            }
        }
        
        Debug.Log($"[Cursor Fix] Fixed {fixedCount} cursor textures. The warnings should be gone now!");
    }

    [MenuItem("Tools/Generate Blurred Background")]
    public static void GenerateBlurredBackground()
    {
        string inputPath = "Assets/Tiny Swords/bg.png";
        string outputPath = "Assets/Tiny Swords/bg_blurred.png";

        Texture2D original = AssetDatabase.LoadAssetAtPath<Texture2D>(inputPath);
        if (original == null)
        {
            Debug.LogError("Original background not found!");
            return;
        }

        // We need to read pixels, so let's make a readable copy if it's not
        string assetPath = AssetDatabase.GetAssetPath(original);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D blurred = BlurTexture(original, 8); // radius

        byte[] bytes = blurred.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);
        AssetDatabase.ImportAsset(outputPath);

        // Restore original setting
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        // Set blurred image to Sprite
        TextureImporter blurredImporter = (TextureImporter)AssetImporter.GetAtPath(outputPath);
        blurredImporter.textureType = TextureImporterType.Sprite;
        blurredImporter.spriteImportMode = SpriteImportMode.Single;
        blurredImporter.SaveAndReimport();

        Debug.Log("Blurred background generated at " + outputPath);
    }

    private static Texture2D BlurTexture(Texture2D source, int radius)
    {
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] sourcePixels = source.GetPixels();
        Color[] newPixels = new Color[sourcePixels.Length];

        int width = source.width;
        int height = source.height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                int count = 0;

                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            Color c = sourcePixels[ny * width + nx];
                            r += c.r;
                            g += c.g;
                            b += c.b;
                            a += c.a;
                            count++;
                        }
                    }
                }

                newPixels[y * width + x] = new Color(r / count, g / count, b / count, a / count);
            }
        }

        result.SetPixels(newPixels);
        result.Apply();
        return result;
    }
}
