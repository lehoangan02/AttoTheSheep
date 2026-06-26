using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AttoTheSheep.Core;
using AttoTheSheep.UI.Shared;

public class LoadingScreenGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Loading Screen")]
    public static void GenerateLoadingScreen()
    {
        // 1. Create Canvas
        GameObject root = new GameObject("LoadingCanvas");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        root.AddComponent<GraphicRaycaster>();
        
        // Add Manager
        LoadingManager manager = root.AddComponent<LoadingManager>();
        manager.loadingCanvas = root;
        
        // 2. Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f); // Mờ 70%
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // 3. Sheep
        GameObject sheepObj = new GameObject("SheepImage");
        sheepObj.transform.SetParent(root.transform, false);
        Image sheepImage = sheepObj.AddComponent<Image>();
        
        // Lấy Sprite con cừu
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Tiny Swords/Pawn and Resources/Meat/Sheep/Sheep_Move.png");
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                sheepImage.sprite = s;
                sheepImage.SetNativeSize();
                sheepObj.GetComponent<RectTransform>().localScale = new Vector3(3, 3, 3); // X3 cho to
                break;
            }
        }
        
        RectTransform sheepRect = sheepObj.GetComponent<RectTransform>();
        sheepRect.anchoredPosition = new Vector2(0, 0); // Giữa màn hình
        
        SheepLoadingAnimation anim = sheepObj.AddComponent<SheepLoadingAnimation>();
        
        // Load all sprites from Sheep_Move.png and assign them to runFrames
        System.Collections.Generic.List<Sprite> runSprites = new System.Collections.Generic.List<Sprite>();
        foreach (Object asset in assets)
        {
            if (asset is Sprite s)
            {
                runSprites.Add(s);
            }
        }
        
        if (runSprites.Count > 0)
        {
            anim.runFrames = runSprites.ToArray();
            anim.frameRate = 12f;
        }
        
        // 4. Text
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(root.transform, false);
        TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
        textTmp.text = "Loading...";
        textTmp.alignment = TextAlignmentOptions.Center;
        textTmp.fontSize = 50;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        // Neo ở cạnh dưới màn hình
        textRect.anchorMin = new Vector2(0.5f, 0);
        textRect.anchorMax = new Vector2(0.5f, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.anchoredPosition = new Vector2(0, 250); // Cách lề dưới 250px
        textRect.sizeDelta = new Vector2(1000, 100);
        
        manager.statusText = textTmp;
        
        // Focus vào object vừa tạo
        Selection.activeGameObject = root;
        Debug.Log("Loading Screen Generated successfully!");
    }
}
