using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click tool that builds the KhoaMenu scene UI from scratch using Tiny Swords assets.
///
/// Layout:
///  - bg.png stretched as background
///  - 3 cloud buttons (New Game / Continue / Multiplayer) aligned LEFT side where clouds are
///  - Settings gear icon in TOP-RIGHT corner
///  - Wood Table settings panel in MID-RIGHT (hidden by default, toggle via settings icon)
///  - Decorative assets: small ribbons, swords, bars scattered around
///  - Black fade overlay on top for scene transitions
/// </summary>
public static class KhoaMenuSetup
{
    // ─── Cloud button positions ───────────────────────────────────────────
    // Values read directly from Unity scene after manual adjustment.
    // anchoredPosition (from canvas center), sizeDelta.
    private static readonly Vector2 CloudTop    = new Vector2(-604f,    3f);
    private static readonly Vector2 CloudMiddle = new Vector2(-598f, -164f);
    private static readonly Vector2 CloudBottom = new Vector2(-600f, -336f);
    private static readonly Vector2 CloudSize   = new Vector2( 370f,   95f);

    // ─── Settings panel ──────────────────────────────────────────────────
    // Values read from Unity scene (880x800, centered).
    private static readonly Vector2 SettingsPanelPos  = new Vector2(0f, 0f);
    private static readonly Vector2 SettingsPanelSize = new Vector2(880f, 800f);

    // ─── Settings icon position (top-right corner) ────────────────────────────
    private static readonly Vector2 SettingsIconPos = new Vector2(-60f, -60f); // from top-right anchor

    [MenuItem("Tools/Setup KhoaMenu UI")]
    public static void SetupUI()
    {
        Scene active = EditorSceneManager.GetActiveScene();
        if (active.name != "KhoaMenu")
        {
            Debug.LogError("[KhoaMenuSetup] Please open the KhoaMenu scene first!");
            return;
        }

        // ── 0. Nuke ALL previously generated objects so we always start fresh ────
        DestroyIfExists("MainMenuRoot");       // old UIBuilder output
        DestroyIfExists("TitleBanner");
        DestroyIfExists("NewGameButton");
        DestroyIfExists("ContinueButton");
        DestroyIfExists("MultiplayerButton");
        DestroyIfExists("Decor");
        DestroyIfExists("MenuManagers");       // old duplicate manager
        DestroyIfExists("Decorations");        // our decorations layer
        DestroyIfExists("SettingsPanel");      // our settings panel
        DestroyIfExists("SettingsIconButton"); // our settings icon
        DestroyIfExists("FadeOverlay");        // fade overlay — MUST destroy to avoid CanvasGroup error

        // Remove UIBuilder component from MainMenuManager so it never runs again
        GameObject mainMenuMgr = GameObject.Find("MainMenuManager");
        if (mainMenuMgr != null)
        {
            var builder = mainMenuMgr.GetComponent<MainMenuUIBuilder>();
            if (builder != null) Object.DestroyImmediate(builder);
        }

        // ── 1. Canvas ────────────────────────────────────────────────────────
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas.sortingOrder = 0;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        // ── 2. EventSystem ───────────────────────────────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── 3. Background ────────────────────────────────────────────────────
        var bg = GetOrCreate("Background", canvas.transform);
        bg.transform.SetAsFirstSibling();
        var bgImg = GetOrAddComponent<Image>(bg);
        var bgSprite = LoadSprite("Assets/Tiny Swords/bg.png");
        if (bgSprite != null) bgImg.sprite = bgSprite;
        bgImg.color = Color.white;
        StretchFull(bg.GetComponent<RectTransform>());

        // ── 4. Decorations layer (behind buttons) ───────────────────────────
        var decorLayer = GetOrCreate("Decorations", canvas.transform);
        ClearChildren(decorLayer.transform);

        // Sword top-right (decorative, behind sheep)
        AddDecor(decorLayer.transform, "Sword_TL",
            "Assets/Tiny Swords/UI Elements/Swords/Swords 1.png",
            new Vector2(-820f, 180f), new Vector2(90f, 90f), 30f);

        AddDecor(decorLayer.transform, "Sword_BL",
            "Assets/Tiny Swords/UI Elements/Swords/Swords 3.png",
            new Vector2(-820f, -260f), new Vector2(90f, 90f), -20f);

        // Small ribbons as label strips above each cloud
        AddDecor(decorLayer.transform, "Ribbon_Top",
            "Assets/Tiny Swords/UI Elements/Ribbons/SmallRibbons 1.png",
            new Vector2(-560f, 210f), new Vector2(220f, 50f), 0f);

        // Bar decoration at bottom
        AddDecor(decorLayer.transform, "Bar_Bottom",
            "Assets/Tiny Swords/UI Elements/Bars/SmallBar_Base.png",
            new Vector2(0f, -500f), new Vector2(600f, 40f), 0f);

        // ── 5. Cloud Buttons ─────────────────────────────────────────────────
        // Load font and button sprite
        TMP_FontAsset font = FindFirstAsset<TMP_FontAsset>("t:TMP_FontAsset");
        Sprite btnSprite   = LoadSprite("Assets/Tiny Swords/UI Elements/Buttons/BigBlueButton_Regular.png");
        Sprite btnSpriteRed = LoadSprite("Assets/Tiny Swords/UI Elements/Buttons/BigRedButton_Regular.png");

        Sprite iconNew   = LoadSprite("Assets/Tiny Swords/UI Elements/Icons/Icon_05.png");
        Sprite iconCont  = LoadSprite("Assets/Tiny Swords/UI Elements/Icons/Icon_02.png");
        Sprite iconMulti = LoadSprite("Assets/Tiny Swords/UI Elements/Icons/Icon_06.png");

        var newGameBtn    = CreateCloudButton("NewGameButton",    canvas.transform, "New Game",    CloudTop,    btnSprite,    font, iconNew,   true);
        var continueBtn   = CreateCloudButton("ContinueButton",   canvas.transform, "Continue",    CloudMiddle, btnSprite,    font, iconCont,  false);
        var multiplayerBtn = CreateCloudButton("MultiplayerButton", canvas.transform, "Multiplayer", CloudBottom, btnSpriteRed, font, iconMulti, false);

        // ── 6. Settings Gear Icon (top-right corner) ─────────────────────────
        Sprite gearIcon = LoadSprite("Assets/Tiny Swords/UI Elements/Icons/Icon_09.png");
        var settingsIconBtn = CreateIconButton("SettingsIconButton", canvas.transform, gearIcon, SettingsIconPos);

        // ── 7. Settings Panel (Wood Table, hidden by default) ─────────────────
        var settingsPanel = BuildSettingsPanel(canvas.transform, font);

        // ── 8. Fade Overlay ──────────────────────────────────────────────────
        // Always create fresh (destroyed above) to avoid CanvasGroup missing component errors.
        var fadeGo = new GameObject("FadeOverlay", typeof(RectTransform));
        fadeGo.transform.SetParent(canvas.transform, false);
        fadeGo.transform.SetAsLastSibling();

        var fadeImg = fadeGo.AddComponent<Image>();
        fadeImg.color         = new Color(0f, 0f, 0f, 0f); // transparent — alpha animated at runtime
        fadeImg.raycastTarget = true;
        StretchFull(fadeGo.GetComponent<RectTransform>());

        var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
        fadeGroup.alpha          = 0f;
        fadeGroup.blocksRaycasts = false;

        // ── 9. Wire up MainMenuController ────────────────────────────────────
        if (mainMenuMgr == null) mainMenuMgr = new GameObject("MainMenuManager");
        var controller = GetOrAddComponent<MainMenuController>(mainMenuMgr);
        controller.Setup(newGameBtn, continueBtn, multiplayerBtn,
                         settingsIconBtn, settingsPanel, fadeGroup);

        // Mark scene dirty so Unity knows to save
        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[KhoaMenuSetup] ✓ Done! Press Play to test.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cloud Button
    // ─────────────────────────────────────────────────────────────────────────
    private static Button CreateCloudButton(string goName, Transform parent, string label,
        Vector2 anchoredPos, Sprite btnSprite, TMP_FontAsset font, Sprite iconSprite, bool interactable)
    {
        var go = GetOrCreate(goName, parent);
        ClearChildren(go.transform);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = CloudSize;
        rect.anchoredPosition = anchoredPos;
        rect.localScale = Vector3.one;

        var img = GetOrAddComponent<Image>(go);
        img.sprite = btnSprite;
        img.type   = btnSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color  = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.9f);

        var btn = GetOrAddComponent<Button>(go);
        btn.transition  = Selectable.Transition.None; // handled by MenuButtonAnimator
        btn.interactable = interactable;

        // Use the existing smooth MenuButtonAnimator (already has hover + entrance animation)
        if (go.GetComponent<MenuButtonAnimator>() == null)
            go.AddComponent<MenuButtonAnimator>();

        // Icon (left side)
        if (iconSprite != null)
        {
            var iconGo  = GetOrCreate("Icon", go.transform);
            var iconImg = GetOrAddComponent<Image>(iconGo);
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            iconImg.raycastTarget = false;
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin        = new Vector2(0f, 0.5f);
            iconRect.anchorMax        = new Vector2(0f, 0.5f);
            iconRect.pivot            = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta        = new Vector2(48f, 48f);
            iconRect.anchoredPosition = new Vector2(32f, 0f);
        }

        // Label text
        var textGo   = GetOrCreate("Text", go.transform);
        var textComp = GetOrAddComponent<TextMeshProUGUI>(textGo);
        textComp.text      = label;
        textComp.fontSize  = 24;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color     = interactable ? Color.white : new Color(0.85f, 0.85f, 0.85f);
        textComp.font      = font;
        textComp.raycastTarget = false;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(50f, 0f);  // leave space for icon
        textRect.offsetMax = Vector2.zero;

        return btn;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Settings Gear Icon Button (top-right corner)
    // ─────────────────────────────────────────────────────────────────────────
    private static Button CreateIconButton(string goName, Transform parent, Sprite icon, Vector2 anchoredPos)
    {
        var go = GetOrCreate(goName, parent);
        ClearChildren(go.transform);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot     = Vector2.one;
        rect.sizeDelta        = new Vector2(72f, 72f);
        rect.anchoredPosition = anchoredPos;
        rect.localScale = Vector3.one;

        // Tiny round blue button as background
        var bgSprite = LoadSprite("Assets/Tiny Swords/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
        var img = GetOrAddComponent<Image>(go);
        img.sprite = bgSprite;
        img.type   = Image.Type.Simple;
        img.color  = Color.white;

        var btn = GetOrAddComponent<Button>(go);
        btn.transition = Selectable.Transition.None;
        if (go.GetComponent<MenuButtonAnimator>() == null)
            go.AddComponent<MenuButtonAnimator>();

        // Icon on top
        if (icon != null)
        {
            var iconGo  = GetOrCreate("GearIcon", go.transform);
            var iconImg = GetOrAddComponent<Image>(iconGo);
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10f, 10f);
            iconRect.offsetMax = new Vector2(-10f, -10f);
        }

        return btn;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Settings Panel (Wood Table)
    // ─────────────────────────────────────────────────────────────────────────
    private static GameObject BuildSettingsPanel(Transform canvasTransform, TMP_FontAsset font)
    {
        var panel = GetOrCreate("SettingsPanel", canvasTransform);
        ClearChildren(panel.transform);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta        = SettingsPanelSize;
        rect.anchoredPosition = SettingsPanelPos;
        rect.localScale = Vector3.one;

        // Wood Table as panel background
        var woodSprite = LoadSprite("Assets/Tiny Swords/UI Elements/Wood Table/WoodTable.png");
        var bgImg = GetOrAddComponent<Image>(panel);
        bgImg.sprite = woodSprite;
        bgImg.type   = Image.Type.Sliced;
        bgImg.color  = Color.white;

        // Title ribbon inside panel
        var ribbonGo  = GetOrCreate("PanelTitle", panel.transform);
        var ribbonImg = GetOrAddComponent<Image>(ribbonGo);
        ribbonImg.sprite = LoadSprite("Assets/Tiny Swords/UI Elements/Ribbons/SmallRibbons 2.png");
        ribbonImg.color  = Color.white;
        ribbonImg.preserveAspect = true;
        ribbonImg.raycastTarget  = false;
        var ribbonRect = ribbonGo.GetComponent<RectTransform>();
        ribbonRect.anchorMin        = new Vector2(0.5f, 1f);
        ribbonRect.anchorMax        = new Vector2(0.5f, 1f);
        ribbonRect.pivot            = new Vector2(0.5f, 0.5f);
        ribbonRect.sizeDelta        = new Vector2(300f, 60f);
        ribbonRect.anchoredPosition = new Vector2(0f, 20f);

        var titleTextGo   = GetOrCreate("TitleText", ribbonGo.transform);
        var titleTextComp = GetOrAddComponent<TextMeshProUGUI>(titleTextGo);
        titleTextComp.text      = "SETTINGS";
        titleTextComp.fontSize  = 22;
        titleTextComp.alignment = TextAlignmentOptions.Center;
        titleTextComp.color     = Color.white;
        titleTextComp.font      = font;
        titleTextComp.raycastTarget = false;
        StretchFull(titleTextGo.GetComponent<RectTransform>());

        // Settings rows: [icon] [label] [small button / icon]
        var settings = new (string label, string iconPath, string valuePath)[]
        {
            ("Sound",    "Assets/Tiny Swords/UI Elements/Icons/Icon_11.png", "Assets/Tiny Swords/UI Elements/Buttons/TinyRoundBlueButton.png"),
            ("Music",    "Assets/Tiny Swords/UI Elements/Icons/Icon_12.png", "Assets/Tiny Swords/UI Elements/Buttons/TinyRoundBlueButton.png"),
            ("Language", "Assets/Tiny Swords/UI Elements/Icons/Icon_03.png", "Assets/Tiny Swords/UI Elements/Buttons/SmallBlueSquareButton_Regular.png"),
            ("Credits",  "Assets/Tiny Swords/UI Elements/Icons/Icon_10.png", "Assets/Tiny Swords/UI Elements/Buttons/SmallRedRoundButton_Regular.png"),
        };

        float startY = 220f;  // rows start near top of 800px-tall panel
        float rowH   = 120f;  // spacing for 4 rows inside 800px panel
        for (int i = 0; i < settings.Length; i++)
        {
            var (label, iconPath, valuePath) = settings[i];
            float y = startY - i * rowH;
            BuildSettingsRow(panel.transform, i, label, iconPath, valuePath, font, y);
        }

        panel.SetActive(false); // hidden by default
        return panel;
    }

    private static void BuildSettingsRow(Transform parent, int idx, string label,
        string iconPath, string btnPath, TMP_FontAsset font, float y)
    {
        var row = GetOrCreate($"Row_{label}", parent);
        ClearChildren(row.transform);

        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin        = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax        = new Vector2(0.5f, 0.5f);
        rowRect.pivot            = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta        = new Vector2(600f, 80f);   // wider row for the bigger panel
        rowRect.anchoredPosition = new Vector2(0f, y);

        // Icon (bigger)
        var iconGo  = GetOrCreate("Icon", row.transform);
        var iconImg = GetOrAddComponent<Image>(iconGo);
        iconImg.sprite = LoadSprite(iconPath);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin        = new Vector2(0f, 0.5f);
        iconRect.anchorMax        = new Vector2(0f, 0.5f);
        iconRect.pivot            = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta        = new Vector2(64f, 64f);   // bigger icon
        iconRect.anchoredPosition = new Vector2(40f, 0f);

        // Label (bigger font)
        var labelGo  = GetOrCreate("Label", row.transform);
        var labelTmp = GetOrAddComponent<TextMeshProUGUI>(labelGo);
        labelTmp.text      = label;
        labelTmp.fontSize  = 26;                             // bigger font size
        labelTmp.alignment = TextAlignmentOptions.Left;
        labelTmp.color     = new Color(0.95f, 0.9f, 0.8f);
        labelTmp.font      = font;
        labelTmp.raycastTarget = false;
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0f, 0.5f);
        labelRect.anchorMax        = new Vector2(0f, 0.5f);
        labelRect.pivot            = new Vector2(0f, 0.5f);
        labelRect.sizeDelta        = new Vector2(300f, 60f);
        labelRect.anchoredPosition = new Vector2(85f, 0f);

        // Action button (bigger)
        var btnGo  = GetOrCreate("ActionBtn", row.transform);
        var btnImg = GetOrAddComponent<Image>(btnGo);
        btnImg.sprite = LoadSprite(btnPath);
        btnImg.type   = Image.Type.Simple;
        btnImg.preserveAspect = true;
        var btnComp = GetOrAddComponent<Button>(btnGo);
        btnComp.transition = Selectable.Transition.ColorTint;
        if (btnGo.GetComponent<MenuButtonAnimator>() == null)
            btnGo.AddComponent<MenuButtonAnimator>();
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin        = new Vector2(1f, 0.5f);
        btnRect.anchorMax        = new Vector2(1f, 0.5f);
        btnRect.pivot            = new Vector2(1f, 0.5f);
        btnRect.sizeDelta        = new Vector2(80f, 56f);    // bigger action button
        btnRect.anchoredPosition = new Vector2(-30f, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Decorative image helper
    // ─────────────────────────────────────────────────────────────────────────
    private static void AddDecor(Transform parent, string name, string spritePath,
        Vector2 pos, Vector2 size, float rotZ)
    {
        var go   = GetOrCreate(name, parent);
        var img  = GetOrAddComponent<Image>(go);
        img.sprite = LoadSprite(spritePath);
        img.preserveAspect = true;
        img.raycastTarget  = false;
        img.color = Color.white;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0.5f);
        r.anchorMax        = new Vector2(0.5f, 0.5f);
        r.pivot            = new Vector2(0.5f, 0.5f);
        r.sizeDelta        = size;
        r.anchoredPosition = pos;
        r.localEulerAngles = new Vector3(0f, 0f, rotZ);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utility helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static GameObject GetOrCreate(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    private static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    private static void StretchFull(RectTransform r)
    {
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.offsetMin        = Vector2.zero;
        r.offsetMax        = Vector2.zero;
        r.localScale       = Vector3.one;
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);

    private static T FindFirstAsset<T>(string filter) where T : Object
    {
        var guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
