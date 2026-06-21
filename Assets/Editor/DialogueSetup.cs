using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools > Setup Dialogue System
///
/// Final layout (bottom of screen, full width, ~380px tall):
///
///  ┌──────────────────────────────────────────────────────────────┐
///  │ ┌────────────┐ [SmallRibbons: Speaker Name]                  │
///  │ │   Avatar   │ dialogue text streams here...                 │
///  │ │  (peeking  │                                               │
///  │ │   above)   │                             [Skip]  [Next >>] │
///  └─┴────────────┴───────────────────────────────────────────────┘
///
/// KEY FIXES:
///   - Root must have RectTransform before saving prefab.
///   - Use UnityEventTools.AddPersistentListener (serialised) not AddListener (runtime-only).
///   - No ▶ character (not in LiberationSans).
/// </summary>
public static class DialogueSetup
{
    // ── Asset paths ────────────────────────────────────────────────────────────
    private const string PrefabPath   = "Assets/Resources/Prefabs/DLG_Manager.prefab";
    private const string MockDataPath = "Assets/Scripts/Core/Dialogue/MockDialogue.asset";

    private const string SpecialPaperPath = "Assets/Tiny Swords/UI Elements/Papers/SpecialPaper.png";
    private const string RibbonPath       = "Assets/Tiny Swords/UI Elements/Ribbons/SmallRibbons 1.png";
    private const string BtnBluePath      = "Assets/Tiny Swords/UI Elements/Buttons/BigBlueButton_Regular.png";
    private const string BtnRedPath       = "Assets/Tiny Swords/UI Elements/Buttons/BigRedButton_Regular.png";
    private const string AvatarPath       = "Assets/Tiny Swords/UI Elements/Human Avatars/Avatars_06.png";

    // ── Panel sizing constants (easy to tweak) ─────────────────────────────────
    private const float PanelH     = 380f;   // total panel height in px
    private const float AvatarW    = 210f;   // avatar frame width
    private const float AvatarH    = 240f;   // avatar frame height (overlaps panel top)
    private const float AvatarOverlap = 80f; // how many px avatar extends ABOVE panel top
    private const float AvatarLeftPad = 18f;
    private const float ContentLeft = 238f;  // text/buttons start right of avatar frame
    private const float BtnW       = 130f;
    private const float BtnH       = 56f;

    // ──────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Dialogue System")]
    public static void SetupDialogueSystem()
    {
        try
        {
            var mockData = EnsureMockData();
            BuildPrefab();
            PlaceInScene(mockData);
            Debug.Log("[DialogueSetup] Done. Ctrl+S → Play → click 'Test Dialogue' button.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueSetup] FAILED: {e}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 1. Mock dialogue data
    // ──────────────────────────────────────────────────────────────────────────
    private static DialogueData EnsureMockData()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(MockDataPath);
        if (existing != null) return existing;

        var d           = ScriptableObject.CreateInstance<DialogueData>();
        d.speakerName   = "Old Shepherd";
        d.speakerAvatar = Spr(AvatarPath);
        d.lines         = new[]
        {
            "Ah, young one! The fields have been restless ever since the wolves started circling the east meadow.",
            "My sheep... Atto wandered off chasing a butterfly again. The others followed, of course.",
            "Find them and bring them back before nightfall. Watch out for the wolves -- they have grown bold lately.",
            "The path through the Whispering Grove is shorter. The route around the lake is safer. Your choice, traveller."
        };

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MockDataPath)!);
        AssetDatabase.CreateAsset(d, MockDataPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[DialogueSetup] Created mock dialogue asset.");
        return d;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Build prefab
    // ──────────────────────────────────────────────────────────────────────────
    private static void BuildPrefab()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs");

        // Prevent overwriting manual UI changes if the prefab already exists.
        if (System.IO.File.Exists(PrefabPath))
        {
            Debug.Log("[DialogueSetup] Prefab already exists. Skipping generation to preserve your manual UI tweaks.");
            return;
        }

        // ── ROOT: needs RectTransform BEFORE SaveAsPrefabAsset so it ──────────
        //    can be properly stretched inside the Canvas on instantiation.
        var rootGo  = new GameObject("DLG_Manager");
        var rootRt  = rootGo.AddComponent<RectTransform>();          // <-- CRITICAL
        // Set a default 1080p size so it looks correct in Prefab Edit Mode
        rootRt.sizeDelta = new Vector2(1920f, 1080f);
        var manager = rootGo.AddComponent<DialogueManager>();

        // ── PANEL CHILD: this is what gets toggled ─────────────────────────────
        // Pinned to BOTTOM of canvas, full width, PanelH pixels tall.
        var panel   = UI("DLG_Panel", rootGo.transform, typeof(Image));
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta        = new Vector2(0f, PanelH);

        var panelImg = panel.GetComponent<Image>();
        var bgSpr    = Spr(SpecialPaperPath);
        if (bgSpr != null)
        {
            panelImg.sprite = bgSpr;
            panelImg.type   = Image.Type.Sliced;
            panelImg.color  = Color.white;
        }
        else
        {
            panelImg.color = new Color(0.10f, 0.07f, 0.04f, 0.96f);
        }
        panelImg.raycastTarget = true;

        // ── AVATAR: anchored to top-left of panel, overlaps above ─────────────
        // pivot = bottom-left so positive anchoredPosition.y moves it upward.
        // overlap = AvatarOverlap px above the panel top edge.
        var avatarFrame   = UI("AvatarFrame", panel.transform, typeof(Image));
        var avatarFrameRt = avatarFrame.GetComponent<RectTransform>();
        avatarFrameRt.anchorMin        = new Vector2(0f, 1f);
        avatarFrameRt.anchorMax        = new Vector2(0f, 1f);
        avatarFrameRt.pivot            = new Vector2(0f, 0f);
        avatarFrameRt.anchoredPosition = new Vector2(AvatarLeftPad, -PanelH + AvatarOverlap);
        // The frame height is AvatarH, of which AvatarOverlap px are below panel top.
        // So: anchoredPosition.y = -(PanelH - (PanelH - AvatarH + AvatarOverlap))
        //   = -(PanelH - AvatarH) - AvatarOverlap  ... wait let's think more simply:
        // We want the BOTTOM of the avatar frame to sit at y = -PanelH + something,
        // and the TOP at y = -PanelH + AvatarH = +AvatarOverlap above panel top.
        // With pivot at bottom-left and anchor at panel top-left:
        //   anchoredPosition.y = 0 means bottom of frame is at panel top.
        //   We want bottom at -(PanelH - AvatarH - AvatarOverlap) from panel top,
        //   i.e., the frame sits inside the panel except for the overlap part.
        avatarFrameRt.anchoredPosition = new Vector2(AvatarLeftPad, -(AvatarH - AvatarOverlap));
        avatarFrameRt.sizeDelta        = new Vector2(AvatarW, AvatarH);

        var avatarFrameImg = avatarFrame.GetComponent<Image>();
        avatarFrameImg.color = new Color(0.08f, 0.05f, 0.02f, 0.85f);  // dark tinted bg

        // Inner avatar image
        var avatarInner   = UI("AvatarImage", avatarFrame.transform, typeof(Image));
        var avatarInnerRt = avatarInner.GetComponent<RectTransform>();
        avatarInnerRt.anchorMin = Vector2.zero;
        avatarInnerRt.anchorMax = Vector2.one;
        avatarInnerRt.offsetMin = new Vector2(4f, 4f);
        avatarInnerRt.offsetMax = new Vector2(-4f, -4f);
        var avatarImgC          = avatarInner.GetComponent<Image>();
        avatarImgC.sprite         = Spr(AvatarPath);
        avatarImgC.preserveAspect = true;

        // ── SPEAKER NAME RIBBON: top of content area, right of avatar ─────────
        // Positioned above the panel top edge (overlapping upward like avatar).
        var nameRibbon   = UI("SpeakerNameRibbon", panel.transform, typeof(Image));
        var nameRibbonRt = nameRibbon.GetComponent<RectTransform>();
        nameRibbonRt.anchorMin        = new Vector2(0f, 1f);
        nameRibbonRt.anchorMax        = new Vector2(0f, 1f);
        nameRibbonRt.pivot            = new Vector2(0f, 0f);
        nameRibbonRt.anchoredPosition = new Vector2(ContentLeft, 4f);  // 4px above panel top
        nameRibbonRt.sizeDelta        = new Vector2(200f, 36f);

        var nameRibbonImg = nameRibbon.GetComponent<Image>();
        var ribbonSpr     = Spr(RibbonPath);
        if (ribbonSpr != null) { nameRibbonImg.sprite = ribbonSpr; nameRibbonImg.type = Image.Type.Sliced; }
        nameRibbonImg.color = Color.white;

        var nameTxtGo   = UI("SpeakerNameText", nameRibbon.transform, typeof(TextMeshProUGUI));
        var nameTxtRt   = nameTxtGo.GetComponent<RectTransform>();
        nameTxtRt.anchorMin = Vector2.zero;
        nameTxtRt.anchorMax = Vector2.one;
        nameTxtRt.offsetMin = new Vector2(18f, 2f);
        nameTxtRt.offsetMax = new Vector2(-18f, -2f);
        var nameTmp         = nameTxtGo.GetComponent<TextMeshProUGUI>();
        nameTmp.text          = "Speaker";
        nameTmp.fontSize      = 18f;
        nameTmp.fontStyle     = FontStyles.Bold;
        nameTmp.color         = Color.white;
        nameTmp.alignment     = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget = false;

        // ── DIALOGUE TEXT BOX ─────────────────────────────────────────────────
        var dlgTxt   = UI("DialogueText", panel.transform, typeof(TextMeshProUGUI));
        var dlgTxtRt = dlgTxt.GetComponent<RectTransform>();
        dlgTxtRt.anchorMin = new Vector2(0f, 0f);
        dlgTxtRt.anchorMax = new Vector2(1f, 1f);
        dlgTxtRt.offsetMin = new Vector2(ContentLeft, BtnH + 14f);  // left of avatar, above buttons
        dlgTxtRt.offsetMax = new Vector2(-14f, -14f);               // right & top margin

        var dlgTmp              = dlgTxt.GetComponent<TextMeshProUGUI>();
        dlgTmp.text             = "";
        dlgTmp.fontSize         = 22f;
        dlgTmp.color            = new Color(0.95f, 0.90f, 0.78f, 1f);
        dlgTmp.alignment        = TextAlignmentOptions.TopLeft;
        dlgTmp.overflowMode     = TextOverflowModes.Truncate;
        dlgTmp.textWrappingMode = TextWrappingModes.Normal;
        dlgTmp.raycastTarget    = false;

        // ── BUTTON AREA: bottom-right ──────────────────────────────────────────
        var btnArea   = UI("ButtonArea", panel.transform, typeof(RectTransform));
        var btnAreaRt = btnArea.GetComponent<RectTransform>();
        btnAreaRt.anchorMin        = new Vector2(1f, 0f);
        btnAreaRt.anchorMax        = new Vector2(1f, 0f);
        btnAreaRt.pivot            = new Vector2(1f, 0f);
        btnAreaRt.anchoredPosition = new Vector2(-16f, 12f);
        btnAreaRt.sizeDelta        = new Vector2((BtnW * 2) + 14f, BtnH);

        var hlg = btnArea.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 14f;
        hlg.childAlignment        = TextAnchor.MiddleRight;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight= false;

        var skipBtn = MakeButton(btnArea.transform, "SkipButton", "Skip",    BtnRedPath,  BtnW, BtnH);
        var nextBtn = MakeButton(btnArea.transform, "NextButton", "Next >>", BtnBluePath, BtnW, BtnH);

        // ── Wire DialogueManager refs ─────────────────────────────────────────
        manager.panelRoot       = panel;         // only the panel child is toggled
        manager.avatarImage     = avatarImgC;
        manager.speakerNameText = nameTmp;
        manager.dialogueText    = dlgTmp;
        manager.skipButton      = skipBtn;
        manager.nextButton      = nextBtn;
        manager.charsPerSecond  = 40f;

        // ── Save ──────────────────────────────────────────────────────────────
        var saved = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
        Object.DestroyImmediate(rootGo);
        AssetDatabase.Refresh();

        if (saved != null)
            Debug.Log($"[DialogueSetup] Prefab saved -> {PrefabPath}");
        else
            Debug.LogError("[DialogueSetup] PrefabUtility.SaveAsPrefabAsset returned null!");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. Place in active scene
    // ──────────────────────────────────────────────────────────────────────────
    private static void PlaceInScene(DialogueData mockData)
    {
        // Find the root canvas
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.isRootCanvas) { canvas = c; break; }
        }
        if (canvas == null)
        {
            Debug.LogWarning("[DialogueSetup] No root Canvas. Open MainMenu.unity first.");
            return;
        }

        // ── Clean up all previous dialogue objects ────────────────────────────
        foreach (var dm in Object.FindObjectsByType<DialogueManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"[DialogueSetup] Removing old '{dm.gameObject.name}'");
            Object.DestroyImmediate(dm.gameObject);
        }
        foreach (var n in new[] { "DLG_Manager", "DLG_TestButton", "DialogueManager_Root",
                                   "DialoguePanel", "DialogueTestButton" })
        {
            var stale = GameObject.Find(n);
            if (stale != null) Object.DestroyImmediate(stale);
        }

        // ── Instantiate prefab ────────────────────────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[DialogueSetup] Prefab not found at {PrefabPath}.");
            return;
        }

        var inst   = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        var instRt = inst.GetComponent<RectTransform>();
        instRt.anchorMin  = Vector2.zero;
        instRt.anchorMax  = Vector2.one;
        instRt.offsetMin  = Vector2.zero;
        instRt.offsetMax  = Vector2.zero;
        instRt.localScale = Vector3.one;
        inst.transform.SetAsLastSibling();

        Debug.Log($"[DialogueSetup] Placed '{inst.name}' under canvas '{canvas.name}'.");

        // ── Test button ───────────────────────────────────────────────────────
        var testGo = UI("DLG_TestButton", canvas.transform, typeof(Image), typeof(Button));
        testGo.transform.SetAsLastSibling();

        var testRt             = testGo.GetComponent<RectTransform>();
        testRt.anchorMin       = new Vector2(0.5f, 1f);
        testRt.anchorMax       = new Vector2(0.5f, 1f);
        testRt.pivot           = new Vector2(0.5f, 1f);
        testRt.anchoredPosition= new Vector2(0f, -20f);   // 20px below top edge
        testRt.sizeDelta       = new Vector2(220f, 56f);

        var testImg = testGo.GetComponent<Image>();
        var blueSpr = Spr(BtnBluePath);
        if (blueSpr != null) { testImg.sprite = blueSpr; testImg.type = Image.Type.Sliced; }
        testImg.color = Color.white;

        var lbl   = UI("Label", testGo.transform, typeof(TextMeshProUGUI));
        var lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = new Vector2(8f, 4f);
        lblRt.offsetMax = new Vector2(-8f, -4f);
        var lblTmp      = lbl.GetComponent<TextMeshProUGUI>();
        lblTmp.text          = "Test Dialogue";   // no special unicode chars
        lblTmp.fontSize      = 21f;
        lblTmp.fontStyle     = FontStyles.Bold;
        lblTmp.color         = Color.white;
        lblTmp.alignment     = TextAlignmentOptions.Center;
        lblTmp.raycastTarget = false;

        // TestDialogueButton wires its own listener in Start() — no UnityEventTools needed.
        var testScript          = testGo.AddComponent<TestDialogueButton>();
        testScript.dialogueData = mockData;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[DialogueSetup] Test button wired with persistent listener. Ctrl+S then Play.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Create a child UI GameObject (always gets RectTransform via UI components).</summary>
    private static GameObject UI(string name, Transform parent, params System.Type[] comps)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        foreach (var c in comps) go.AddComponent(c);
        return go;
    }

    private static Button MakeButton(Transform parent, string goName, string label,
                                     string spritePath, float w, float h)
    {
        var go  = UI(goName, parent, typeof(Image), typeof(Button));
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);

        var img = go.GetComponent<Image>();
        var spr = Spr(spritePath);
        if (spr != null) { img.sprite = spr; img.type = Image.Type.Sliced; }
        img.color = Color.white;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var txt   = UI("Text", go.transform, typeof(TextMeshProUGUI));
        var txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(6f, 4f);
        txtRt.offsetMax = new Vector2(-6f, -4f);
        var tmp         = txt.GetComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 19f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private static Sprite Spr(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (t == null) { Debug.LogWarning($"[DialogueSetup] Asset not found: {path}"); return null; }
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
