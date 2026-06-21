using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class WaitLobbySetup
{
    private const string ScenePath = "Assets/Scenes/WaitLobby.unity";
    private const string PrefabDir = "Assets/Resources/Prefabs";
    private const string RowPrefabPath = "Assets/Resources/Prefabs/PlayerRow.prefab";

    [MenuItem("Tools/Setup Wait Lobby")]
    public static void SetupWaitLobby()
    {
        var scene = SceneManager.GetActiveScene();

        // Find existing Canvas or create one
        var canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGo;
        if (canvas == null)
        {
            canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGo = canvas.gameObject;
        }

        // Event System
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // Try to add new Input System module, fallback to Standalone if it fails
            var inputSystemType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemType != null)
            {
                eventSystem.AddComponent(inputSystemType);
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // --- Background ---
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        bgGo.transform.SetAsFirstSibling();
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/bg_blurred.png");
        bgImg.color = Color.white;

        // Top Black Bar
        var topBarGo = new GameObject("TopBar");
        topBarGo.transform.SetParent(canvasGo.transform, false);
        var topBarRt = topBarGo.AddComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.offsetMin = new Vector2(0, -80);
        topBarRt.offsetMax = new Vector2(0, 0);
        var topBarImg = topBarGo.AddComponent<Image>();
        topBarImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        var lobbyNameTxt = CreateText(topBarGo.transform, "LobbyName", "JOINED LOBBY: MyLobby", Vector2.zero, Vector2.one, 40, TextAlignmentOptions.Center);
        lobbyNameTxt.color = Color.white;
        lobbyNameTxt.fontStyle = FontStyles.Bold;

        // --- Create Panel ---
        var panelGo = new GameObject("MainPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(1000, 700);
        var panelImage = panelGo.AddComponent<Image>();
        panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/UI Elements/Papers/RegularPaper.png");
        panelImage.type = Image.Type.Sliced;

        // Header Ribbon
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(panelGo.transform, false);
        var headerRt = headerGo.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.offsetMin = new Vector2(0, -100);
        headerRt.offsetMax = new Vector2(0, 30);
        var headerImage = headerGo.AddComponent<Image>();
        headerImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/UI Elements/Banners/Banner.png");
        headerImage.type = Image.Type.Sliced;

        // Header Texts
        var playerCountTxt = CreateText(headerGo.transform, "PlayerCount", "4/4", new Vector2(0, 0), new Vector2(0.5f, 1), 36, TextAlignmentOptions.Center);
        playerCountTxt.color = new Color(0.2f, 0.2f, 0.2f);
        var gameModeTxt = CreateText(headerGo.transform, "GameMode", "Conquest", new Vector2(0.5f, 0), new Vector2(1, 1), 36, TextAlignmentOptions.Center);
        gameModeTxt.color = new Color(0.2f, 0.2f, 0.2f);

        // Player List Scroll Area
        var scrollGo = new GameObject("PlayerScroll");
        scrollGo.transform.SetParent(panelGo.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0.15f);
        scrollRt.anchorMax = new Vector2(1, 1);
        scrollRt.offsetMin = new Vector2(50, 0);
        scrollRt.offsetMax = new Vector2(-50, -100);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewRt = viewportGo.AddComponent<RectTransform>();
        viewRt.anchorMin = Vector2.zero;
        viewRt.anchorMax = Vector2.one;
        viewRt.sizeDelta = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(1,1,1,0.01f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;

        var listGo = new GameObject("PlayerListContent");
        listGo.transform.SetParent(viewportGo.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0, 1);
        listRt.anchorMax = new Vector2(1, 1);
        listRt.pivot = new Vector2(0.5f, 1);
        listRt.sizeDelta = new Vector2(0, 0);
        
        var vlg = listGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        scrollRect.viewport = viewRt;
        scrollRect.content = listRt;
        scrollRect.horizontal = false;

        // Build Player Row Prefab
        CreatePlayerRowPrefab();
        
        // Dummy rows for editor preview
        var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        for(int i=0; i<4; i++) 
        {
            var inst = PrefabUtility.InstantiatePrefab(rowPrefab) as GameObject;
            if (inst != null) inst.transform.SetParent(listGo.transform, false);
        }

        // Bottom Bar
        var bottomGo = new GameObject("BottomBar");
        bottomGo.transform.SetParent(panelGo.transform, false);
        var bottomRt = bottomGo.AddComponent<RectTransform>();
        bottomRt.anchorMin = new Vector2(0, 0);
        bottomRt.anchorMax = new Vector2(1, 0.15f);
        bottomRt.offsetMin = new Vector2(0, 0);
        bottomRt.offsetMax = new Vector2(0, 0);

        var leaveBtn = CreateButton(bottomGo.transform, "LeaveBtn", "Leave", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(200, 70), new Vector2(200, 0), true);
        var readyBtn = CreateButton(bottomGo.transform, "ReadyBtn", "Ready/Start", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(200, 70), new Vector2(-200, 0));

        // Confirm Panel
        var confirmGo = new GameObject("ConfirmPanel");
        confirmGo.transform.SetParent(canvasGo.transform, false);
        var confirmRt = confirmGo.AddComponent<RectTransform>();
        confirmRt.anchorMin = Vector2.zero;
        confirmRt.anchorMax = Vector2.one;
        confirmRt.sizeDelta = Vector2.zero;
        var confirmBg = confirmGo.AddComponent<Image>();
        confirmBg.color = new Color(0,0,0,0.8f);
        
        var confirmBoxGo = new GameObject("Box");
        confirmBoxGo.transform.SetParent(confirmGo.transform, false);
        var confirmBoxRt = confirmBoxGo.AddComponent<RectTransform>();
        confirmBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
        confirmBoxRt.anchorMax = new Vector2(0.5f, 0.5f);
        confirmBoxRt.sizeDelta = new Vector2(600, 400);
        var confirmBoxImg = confirmBoxGo.AddComponent<Image>();
        confirmBoxImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/UI Elements/Papers/RegularPaper.png");
        confirmBoxImg.type = Image.Type.Sliced;
        
        var confirmTitle = CreateText(confirmBoxGo.transform, "Title", "Do you want to leave?", new Vector2(0, 0.5f), new Vector2(1, 1f), 36, TextAlignmentOptions.Center);
        confirmTitle.color = new Color(0.2f,0.2f,0.2f);
        var yesBtn = CreateButton(confirmBoxGo.transform, "YesBtn", "Yes", new Vector2(0.2f, 0.3f), new Vector2(0.2f, 0.3f), new Vector2(150, 60), Vector2.zero, true);
        var noBtn = CreateButton(confirmBoxGo.transform, "NoBtn", "No", new Vector2(0.8f, 0.3f), new Vector2(0.8f, 0.3f), new Vector2(150, 60), Vector2.zero);
        
        confirmGo.SetActive(false);

        // Add Controller
        var controller = canvasGo.GetComponent<WaitLobbyController>();
        if (controller == null) controller = canvasGo.AddComponent<WaitLobbyController>();
        
        // Use reflection or serialized object to assign private fields
        var so = new SerializedObject(controller);
        so.FindProperty("lobbyNameText").objectReferenceValue = lobbyNameTxt;
        so.FindProperty("playerCountText").objectReferenceValue = playerCountTxt;
        so.FindProperty("gameModeText").objectReferenceValue = gameModeTxt;
        so.FindProperty("playerListContent").objectReferenceValue = listGo.transform;
        so.FindProperty("playerRowPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        so.FindProperty("leaveButton").objectReferenceValue = leaveBtn.GetComponent<Button>();
        so.FindProperty("readyButton").objectReferenceValue = readyBtn.GetComponent<Button>();
        so.FindProperty("confirmPanel").objectReferenceValue = confirmGo;
        so.FindProperty("confirmYesButton").objectReferenceValue = yesBtn.GetComponent<Button>();
        so.FindProperty("confirmNoButton").objectReferenceValue = noBtn.GetComponent<Button>();

        // Find some avatars
        var guids = AssetDatabase.FindAssets("t:Sprite Avatars", new[] { "Assets/Tiny Swords/UI Elements" });
        var avatars = guids.Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null).ToArray();
        
        var avatarProp = so.FindProperty("mockAvatars");
        avatarProp.arraySize = avatars.Length;
        for (int i = 0; i < avatars.Length; i++)
        {
            avatarProp.GetArrayElementAtIndex(i).objectReferenceValue = avatars[i];
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WaitLobbySetup] Scene generated successfully at " + scene.path);
    }

    private static void CreatePlayerRowPrefab()
    {
        if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

        var rowGo = new GameObject("PlayerRow");
        var rowRt = rowGo.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(900, 80);
        
        var rowImg = rowGo.AddComponent<Image>();
        rowImg.color = new Color(0, 0, 0, 0.3f); // Dark tint for row

        // Avatar
        var avatarGo = new GameObject("Avatar");
        avatarGo.transform.SetParent(rowGo.transform, false);
        var avatarRt = avatarGo.AddComponent<RectTransform>();
        avatarRt.anchorMin = new Vector2(0, 0.5f);
        avatarRt.anchorMax = new Vector2(0, 0.5f);
        avatarRt.sizeDelta = new Vector2(70, 70);
        avatarRt.anchoredPosition = new Vector2(50, 0);
        var avatarImg = avatarGo.AddComponent<Image>();

        // Name
        var nameTxt = CreateText(rowGo.transform, "Name", "Player Name", new Vector2(0, 0), new Vector2(1, 1), 32, TextAlignmentOptions.Left);
        var nameRt = nameTxt.GetComponent<RectTransform>();
        nameRt.offsetMin = new Vector2(100, 0);

        // Host Indicator (Flag)
        var flagGo = new GameObject("HostFlag");
        flagGo.transform.SetParent(rowGo.transform, false);
        var flagRt = flagGo.AddComponent<RectTransform>();
        flagRt.anchorMin = new Vector2(1, 0.5f);
        flagRt.anchorMax = new Vector2(1, 0.5f);
        flagRt.sizeDelta = new Vector2(50, 50);
        flagRt.anchoredPosition = new Vector2(-40, 0);
        var flagTxt = CreateText(flagGo.transform, "Check", "v", Vector2.zero, Vector2.one, 50, TextAlignmentOptions.Center);
        flagTxt.color = Color.green;
        flagTxt.fontStyle = FontStyles.Bold;

        // Kick Button (X)
        var kickGo = new GameObject("KickBtn");
        kickGo.transform.SetParent(rowGo.transform, false);
        var kickRt = kickGo.AddComponent<RectTransform>();
        kickRt.anchorMin = new Vector2(1, 0.5f);
        kickRt.anchorMax = new Vector2(1, 0.5f);
        kickRt.sizeDelta = new Vector2(50, 50);
        kickRt.anchoredPosition = new Vector2(-40, 0);
        var kickImg = kickGo.AddComponent<Image>();
        kickImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/UI Elements/Buttons/SmallRedSquareButton_Regular.png");
        var kickBtn = kickGo.AddComponent<Button>();
        var kickTxt = CreateText(kickGo.transform, "Text", "X", new Vector2(0, 0), new Vector2(1, 1), 36, TextAlignmentOptions.Center);
        kickTxt.color = Color.white;

        var rowUI = rowGo.AddComponent<PlayerRowUI>();
        var so = new SerializedObject(rowUI);
        so.FindProperty("avatarImage").objectReferenceValue = avatarImg;
        so.FindProperty("playerNameText").objectReferenceValue = nameTxt;
        so.FindProperty("hostIndicator").objectReferenceValue = flagGo;
        so.FindProperty("kickButton").objectReferenceValue = kickBtn;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(rowGo, RowPrefabPath);
        Object.DestroyImmediate(rowGo);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        
        // Force font if available
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font != null) tmp.font = font;

        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos, bool isRed = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(isRed ? "Assets/Tiny Swords/UI Elements/Buttons/BigRedButton_Regular.png" : "Assets/Tiny Swords/UI Elements/Buttons/BigBlueButton_Regular.png");
        img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font != null) tmp.font = font;

        return go;
    }
}
