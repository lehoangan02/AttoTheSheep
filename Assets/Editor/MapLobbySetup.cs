using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MapLobbySetup
{
    [MenuItem("Tools/Setup MapLobby UI")]
    public static void SetupUI()
    {
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name != "MapLobby")
        {
            bool switchScene = EditorUtility.DisplayDialog("Wrong Scene", 
                "You are currently in the '" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + "' scene.\n\nThis tool must be run inside the 'MapLobby' scene to avoid overwriting your other menus.\n\nDo you want me to open the MapLobby scene for you now?", "Yes, open MapLobby", "Cancel");
                
            if (switchScene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MapLobby.unity");
            }
            else
            {
                return;
            }
        }

        // 1. Setup Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var existingEs = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingEs != null)
        {
            if (existingEs.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() != null)
            {
                Object.DestroyImmediate(existingEs.gameObject);
                existingEs = null;
            }
        }
        
        if (existingEs == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            var type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type != null) esObj.AddComponent(type);
            else esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Clean previous
        while (canvas.transform.childCount > 0)
        {
            Object.DestroyImmediate(canvas.transform.GetChild(0).gameObject);
        }

        // 2. Load Assets
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/bg_map.png");
        Sprite playerUiSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Player_Coin_EXP.png");
        Sprite bagSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Bag_Icon.png");
        Sprite shopSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Shop_Icon.png");
        Sprite upgradeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Upgrade_Icon.png");

        Sprite lockedBall = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Ball_Locked.png");
        Sprite ball1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Ball1.png");
        Sprite ball2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Ball2.png");
        Sprite ball3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/MapLobby/Ball3.png");

        Sprite backBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/UI Elements/Buttons/BigBlueButton_Regular.png");
        Texture2D cursorDefault = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tiny Swords/UI Elements/Cursors/Cursor_Default.png");
        Texture2D cursorHover = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tiny Swords/UI Elements/Cursors/Cursor_Hover.png");

        // 3. Background
        GameObject bgObj = CreateUIElement("Background", canvas.transform, bgSprite);
        SetRectStretch(bgObj.GetComponent<RectTransform>());

        // 4. Player TopLeft UI
        GameObject playerUiObj = CreateUIElement("PlayerUI", canvas.transform, playerUiSprite);
        SetRectTopLeft(playerUiObj.GetComponent<RectTransform>(), new Vector2(30, -30));

        // 5. Bag Button (Under Player UI)
        GameObject bagObj = CreateUIButton("BagButton", canvas.transform, bagSprite, cursorDefault, cursorHover, 0.4f);
        SetRectTopLeft(bagObj.GetComponent<RectTransform>(), new Vector2(80, -280));

        // 6. Buttons (Bottom Right)
        GameObject backObj = CreateUIButton("BackButton", canvas.transform, backBtnSprite, cursorDefault, cursorHover, 1.0f);
        SetRectBottomRight(backObj.GetComponent<RectTransform>(), new Vector2(-70, 70));
        
        GameObject shopObj = CreateUIButton("ShopButton", canvas.transform, shopSprite, cursorDefault, cursorHover, 0.5f);
        SetRectBottomRight(shopObj.GetComponent<RectTransform>(), new Vector2(-220, 70));

        GameObject upgradeObj = CreateUIButton("UpgradeButton", canvas.transform, upgradeSprite, cursorDefault, cursorHover, 0.5f);
        SetRectBottomRight(upgradeObj.GetComponent<RectTransform>(), new Vector2(-400, 70));

        // 7. Level Nodes
        GameObject node1Obj = CreateUIButton("Node_1", canvas.transform, lockedBall, cursorDefault, cursorHover, 0.45f);
        SetRectCenter(node1Obj.GetComponent<RectTransform>(), new Vector2(-250, -250));

        GameObject node2Obj = CreateUIButton("Node_2", canvas.transform, lockedBall, cursorDefault, cursorHover, 0.45f);
        SetRectCenter(node2Obj.GetComponent<RectTransform>(), new Vector2(120, -100));

        GameObject node3Obj = CreateUIButton("Node_3", canvas.transform, lockedBall, cursorDefault, cursorHover, 0.45f);
        SetRectCenter(node3Obj.GetComponent<RectTransform>(), new Vector2(350, 200));

        // Add Text to Back Button
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObj.transform.SetParent(backObj.transform, false);
        UnityEngine.UI.Text txt = textObj.GetComponent<UnityEngine.UI.Text>();
        txt.text = "BACK";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        SetRectStretch(textObj.GetComponent<RectTransform>());

        // 9. Controller Setup
        MapLobbyController controller = canvas.gameObject.GetComponent<MapLobbyController>();
        if (controller == null) controller = canvas.gameObject.AddComponent<MapLobbyController>();

        controller.bagButton = bagObj.GetComponent<Button>();
        controller.shopButton = shopObj.GetComponent<Button>();
        controller.upgradeButton = upgradeObj.GetComponent<Button>();
        controller.backButton = backObj.GetComponent<Button>();

        controller.levelNodes = new List<Button>
        {
            node1Obj.GetComponent<Button>(),
            node2Obj.GetComponent<Button>(),
            node3Obj.GetComponent<Button>()
        };

        controller.levelUnlockedSprites = new List<Sprite> { ball1, ball2, ball3 };
        controller.levelLockedSprite = lockedBall;

        // 9. Add MapLobby to Build Settings automatically
        var scenes = EditorBuildSettings.scenes;
        bool foundScene = false;
        foreach (var s in scenes)
        {
            if (s.path == "Assets/Scenes/MapLobby.unity")
            {
                foundScene = true;
                break;
            }
        }
        if (!foundScene)
        {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            System.Array.Copy(scenes, newScenes, scenes.Length);
            newScenes[scenes.Length] = new EditorBuildSettingsScene("Assets/Scenes/MapLobby.unity", true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[MapLobby] Automatically added MapLobby to Build Settings!");
        }

        // 10. Mark scene as dirty and save automatically
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[MapLobby] Successfully built and saved UI for MapLobby!");
    }

    private static GameObject CreateUIElement(string name, Transform parent, Sprite sprite, float scale = 1f)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image img = obj.GetComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.SetNativeSize();
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * scale, rt.sizeDelta.y * scale);
            rt.localScale = Vector3.one;
        }
        return obj;
    }

    private static GameObject CreateUIButton(string name, Transform parent, Sprite sprite, Texture2D defCursor, Texture2D hovCursor, float scale = 1f)
    {
        GameObject obj = CreateUIElement(name, parent, sprite, scale);
        obj.AddComponent<Button>();
        MenuButtonAnimator anim = obj.AddComponent<MenuButtonAnimator>();
        anim.HoverScale = 1.1f;
        anim.PressedScale = 0.95f;
        anim.defaultCursorOverride = defCursor;
        anim.hoverCursorOverride = hovCursor;

        return obj;
    }

    private static void SetRectStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRectTopLeft(RectTransform rect, Vector2 offset)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = offset;
    }

    private static void SetRectTopRight(RectTransform rect, Vector2 offset)
    {
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = offset;
    }

    private static void SetRectBottomRight(RectTransform rect, Vector2 offset)
    {
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = offset;
    }

    private static void SetRectCenter(RectTransform rect, Vector2 offset)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = offset;
    }
}
