using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AttoTheSheep.UI.ShopAndInventory;

/// <summary>
/// Editor Tool: Tạo PlayerLobbyHUD cho màn Lobby
/// Menu bar -> Tools -> Build Player Lobby HUD
/// </summary>
public class BuildPlayerLobbyHUD : EditorWindow
{
    private Sprite _avatarSprite;
    private Sprite _goldIconSprite;

    [MenuItem("Tools/Build Player Lobby HUD")]
    public static void ShowWindow()
    {
        var w = GetWindow<BuildPlayerLobbyHUD>("Build Player Lobby HUD");
        w.minSize = new Vector2(380, 240);
        w.Show();
    }

    private void OnEnable() => TryAutoLoadSprites();

    private void TryAutoLoadSprites()
    {
        if (_avatarSprite   == null) _avatarSprite   = FindSprite("Player_Icon");
        if (_goldIconSprite == null) _goldIconSprite = FindSprite("Gold_Resource");
    }

    private Sprite FindSprite(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is Sprite s) return s;
            var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;
        }
        return null;
    }

    private void OnGUI()
    {
        GUILayout.Label("Player Lobby HUD Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool tạo GameObject PlayerLobbyHUD trong Scene.\n" +
            "Sau khi build, kéo nó từ Hierarchy vào Project để Save Prefab.",
            MessageType.Info);
        EditorGUILayout.Space(8);

        _avatarSprite   = (Sprite)EditorGUILayout.ObjectField("Avatar Sprite",    _avatarSprite,   typeof(Sprite), false);
        _goldIconSprite = (Sprite)EditorGUILayout.ObjectField("Gold Icon Sprite", _goldIconSprite, typeof(Sprite), false);

        if (_avatarSprite == null || _goldIconSprite == null)
            EditorGUILayout.HelpBox("Chưa tìm thấy sprite. Bạn có thể tự kéo vào.", MessageType.Warning);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Build HUD vào Scene", GUILayout.Height(36)))
            BuildHUD();
    }

    private void BuildHUD()
    {
        // ── Root — Canvas tự chứa, không phụ thuộc gì trong Scene ────────
        var rootGO = new GameObject("PlayerLobbyHUD");
        Undo.RegisterCreatedObjectUndo(rootGO, "Build PlayerLobbyHUD");

        var canvas          = rootGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        var scaler                    = rootGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;

        rootGO.AddComponent<GraphicRaycaster>();

        // ── HUD Panel ─────────────────────────────────────────────────────
        var panel   = new GameObject("HUDPanel");
        panel.transform.SetParent(rootGO.transform, false);

        var panelRT              = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0f, 1f);
        panelRT.anchorMax        = new Vector2(0f, 1f);
        panelRT.pivot            = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(20f, -20f);
        panelRT.sizeDelta        = new Vector2(230f, 72f);

        var bgImg               = panel.AddComponent<Image>();
        bgImg.color             = new Color(0f, 0f, 0f, 0.50f);
        bgImg.raycastTarget     = false;
        var builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtinSprite != null)
        {
            bgImg.sprite = builtinSprite;
            bgImg.type   = Image.Type.Sliced;
        }

        // ── Avatar ────────────────────────────────────────────────────────
        var avatarGO              = CreateImg("Avatar", panel.transform);
        var avatarRT              = avatarGO.GetComponent<RectTransform>();
        avatarRT.anchorMin        = new Vector2(0f, 0.5f);
        avatarRT.anchorMax        = new Vector2(0f, 0.5f);
        avatarRT.pivot            = new Vector2(0f, 0.5f);
        avatarRT.anchoredPosition = new Vector2(6f, 0f);
        avatarRT.sizeDelta        = new Vector2(62f, 62f);

        var avatarImg             = avatarGO.GetComponent<Image>();
        avatarImg.raycastTarget   = false;
        if (_avatarSprite != null) { avatarImg.sprite = _avatarSprite; avatarImg.preserveAspect = true; }
        else                       { avatarImg.color  = new Color(0.55f, 0.55f, 0.55f); }

        // ── Gold Row ──────────────────────────────────────────────────────
        var goldRow              = new GameObject("GoldRow");
        goldRow.transform.SetParent(panel.transform, false);

        var goldRowRT              = goldRow.AddComponent<RectTransform>();
        goldRowRT.anchorMin        = new Vector2(0f, 0.5f);
        goldRowRT.anchorMax        = new Vector2(1f, 0.5f);
        goldRowRT.pivot            = new Vector2(0f, 0.5f);
        goldRowRT.anchoredPosition = new Vector2(74f, 0f);
        goldRowRT.sizeDelta        = new Vector2(-84f, 40f);

        var hlg                     = goldRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                 = 8f;
        hlg.childAlignment          = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth   = false;
        hlg.childForceExpandHeight  = false;
        hlg.childControlWidth       = false;
        hlg.childControlHeight      = false;

        // Gold Icon
        var iconGO              = CreateImg("GoldIcon", goldRow.transform);
        iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);
        var iconImg             = iconGO.GetComponent<Image>();
        iconImg.raycastTarget   = false;
        if (_goldIconSprite != null) { iconImg.sprite = _goldIconSprite; iconImg.preserveAspect = true; }
        else                         { iconImg.color  = Color.yellow; }
        iconGO.AddComponent<LayoutElement>().preferredWidth = 34f;

        // Gold Text
        var textGO              = new GameObject("GoldText");
        textGO.transform.SetParent(goldRow.transform, false);
        textGO.AddComponent<RectTransform>().sizeDelta = new Vector2(110f, 40f);

        var tmp           = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = "999";
        tmp.fontSize      = 24f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = new Color(1f, 0.92f, 0.3f);
        tmp.alignment     = TextAlignmentOptions.MidlineLeft;
        textGO.AddComponent<LayoutElement>().preferredWidth = 110f;

        // ── Runtime script ────────────────────────────────────────────────
        var hud = rootGO.AddComponent<PlayerLobbyHUDDisplay>();
        var so  = new SerializedObject(hud);
        so.FindProperty("goldText").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = rootGO;
        Debug.Log("[BuildPlayerLobbyHUD] ✅ Done!");
        EditorUtility.DisplayDialog("✅ Hoàn tất!",
            "PlayerLobbyHUD đã được tạo!\n\nKéo từ Hierarchy vào Project/Prefabs để lưu.", "OK");
    }

    private GameObject CreateImg(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>();
        return go;
    }
}
