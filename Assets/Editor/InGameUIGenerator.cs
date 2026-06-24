using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AttoTheSheep.UI;
using AttoTheSheep.UI.InGame;
using System.IO;

namespace AttoTheSheep.Editor
{
    public class InGameUIGenerator
    {
        [MenuItem("Tools/Generate In-Game UI Prefabs")]
        public static void GeneratePrefabs()
        {
            EnsureFolderExists("Assets/Resources/Prefabs/UI");

            GenerateBannerPrefab("WinBannerUI", "YOU WIN!", new Color(0.4f, 0.8f, 0.2f), true);
            GenerateBannerPrefab("LoseBannerUI", "YOU LOSE!", new Color(0.9f, 0.2f, 0.2f), false);
            GeneratePausePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[UI Generator]</color> Tự động tạo 3 Prefab thành công tại Assets/Resources/Prefabs/UI/");
        }

        private static void EnsureFolderExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static Sprite FindSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private static Texture2D FindTexture(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private static GameObject CreateRootCanvas(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            return root;
        }

        private static GameObject CreateImageNode(string name, Transform parent, Sprite sprite, bool isRaycastTarget = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = isRaycastTarget;
            if (sprite != null) img.type = Image.Type.Sliced;
            return go;
        }

        private static TextMeshProUGUI CreateTextNode(string name, Transform parent, string text, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // Luôn tắt Raycast cho Text theo chuẩn
            
            // Tìm font chữ SDF nếu có (mặc định lấy font Skagwae hoặc LiberationSans)
            string[] fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (fontGuids.Length > 0)
            {
                tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
            }
            
            return tmp;
        }

        private static Button CreateHitboxButton(string name, Transform parent, string btnText, Sprite bgSprite)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 80);

            // Background (Raycast OFF theo yêu cầu)
            Image img = btnObj.AddComponent<Image>();
            img.sprite = bgSprite;
            if (bgSprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false; 

            // Button Component
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Âm thanh
            btnObj.AddComponent<UIButtonSound>();

            // Chuột Hover
            HoverCursor hc = btnObj.AddComponent<HoverCursor>();
            hc.defaultHotSpot = new Vector2(22, 17);
            hc.hoverHotSpot = new Vector2(22, 17);
            hc.defaultCursor = FindTexture("Cursor_01");
            hc.hoverCursor = FindTexture("Cursor_02");

            // Hitbox Child (Raycast ON)
            GameObject hitboxObj = new GameObject("Hitbox", typeof(RectTransform), typeof(Image));
            hitboxObj.transform.SetParent(btnObj.transform, false);
            RectTransform hitboxRT = hitboxObj.GetComponent<RectTransform>();
            hitboxRT.anchorMin = Vector2.zero;
            hitboxRT.anchorMax = Vector2.one;
            // Tự động thụt lề 1 chút dựa theo kinh nghiệm viền ảnh Tiny Swords
            hitboxRT.offsetMin = new Vector2(10, 10); 
            hitboxRT.offsetMax = new Vector2(-10, -10); 
            
            Image hitboxImg = hitboxObj.GetComponent<Image>();
            hitboxImg.color = new Color(0, 0, 0, 0); // Trong suốt
            hitboxImg.raycastTarget = true; // Chìa khóa Bubble Up

            // Text Child
            TextMeshProUGUI tmp = CreateTextNode("Text", btnObj.transform, btnText, 30, Color.white);
            RectTransform textRT = tmp.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
            
            // Viền chữ cho đẹp
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;

            return btn;
        }

        private static void GenerateBannerPrefab(string prefabName, string titleText, Color ribbonColor, bool isWin)
        {
            GameObject root = CreateRootCanvas(prefabName);

            // Nền đen mờ Overlay
            GameObject overlay = CreateImageNode("Overlay", root.transform, null, true);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;

            // Bảng gỗ lớn
            GameObject board = CreateImageNode("Board", root.transform, FindSprite("WoodTable"));
            RectTransform boardRT = board.GetComponent<RectTransform>();
            boardRT.sizeDelta = new Vector2(600, 600);

            // Dải băng phía trên (Ribbon)
            GameObject ribbon = CreateImageNode("Ribbon", board.transform, FindSprite("BigRibbons 1"));
            ribbon.GetComponent<Image>().color = ribbonColor;
            RectTransform ribbonRT = ribbon.GetComponent<RectTransform>();
            ribbonRT.anchorMin = new Vector2(0.5f, 1f); ribbonRT.anchorMax = new Vector2(0.5f, 1f);
            ribbonRT.pivot = new Vector2(0.5f, 0.5f);
            ribbonRT.anchoredPosition = new Vector2(0, 0);
            ribbonRT.sizeDelta = new Vector2(500, 120);

            CreateTextNode("Title", ribbon.transform, titleText, 50, Color.white).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // Khung giấy ở giữa
            GameObject paper = CreateImageNode("Paper", board.transform, FindSprite("RegularPaper"));
            RectTransform paperRT = paper.GetComponent<RectTransform>();
            paperRT.sizeDelta = new Vector2(450, 300);
            paperRT.anchoredPosition = new Vector2(0, 20);

            // Ba ngôi sao (Placeholder rỗng)
            GameObject starsRow = new GameObject("StarsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starsRow.transform.SetParent(paper.transform, false);
            starsRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
            starsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);
            HorizontalLayoutGroup hlg = starsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10;

            for (int i = 0; i < 3; i++)
            {
                GameObject star = CreateImageNode("Star_" + i, starsRow.transform, null);
                star.GetComponent<Image>().color = isWin ? Color.yellow : Color.gray; // Placeholder
                LayoutElement le = star.AddComponent<LayoutElement>();
                le.preferredWidth = 60; le.preferredHeight = 60;
            }

            // Chữ Score
            CreateTextNode("ScoreText", paper.transform, "SCORE\n<color=yellow>1000</color>", 35, Color.white).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

            // Nhóm 2 nút bấm
            GameObject buttonsRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonsRow.transform.SetParent(board.transform, false);
            buttonsRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
            HorizontalLayoutGroup btnLayout = buttonsRow.GetComponent<HorizontalLayoutGroup>();
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.spacing = 40;

            Sprite btnSprite = FindSprite("TinySquareBlueButton"); // Nút xanh hoặc vàng
            Button btn1 = CreateHitboxButton(isWin ? "NextLevelBtn" : "RetryBtn", buttonsRow.transform, isWin ? "Next" : "Retry", btnSprite);
            Button btn2 = CreateHitboxButton("MainMenuBtn", buttonsRow.transform, "Menu", btnSprite);

            // Attach Logic Controller
            if (isWin)
            {
                WinBannerController ctrl = root.AddComponent<WinBannerController>();
                var so = new SerializedObject(ctrl);
                so.FindProperty("nextLevelButton").objectReferenceValue = btn1;
                so.FindProperty("mainMenuButton").objectReferenceValue = btn2;
                so.ApplyModifiedProperties();
            }
            else
            {
                LoseBannerController ctrl = root.AddComponent<LoseBannerController>();
                var so = new SerializedObject(ctrl);
                so.FindProperty("retryButton").objectReferenceValue = btn1;
                so.FindProperty("mainMenuButton").objectReferenceValue = btn2;
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/Prefabs/UI/{prefabName}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void GeneratePausePrefab()
        {
            GameObject root = CreateRootCanvas("PauseUI");

            // Nền đen mờ
            GameObject overlay = CreateImageNode("Overlay", root.transform, null, true);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;

            // Bảng gỗ dọc
            GameObject board = CreateImageNode("PausePanel", root.transform, FindSprite("WoodTable"));
            RectTransform boardRT = board.GetComponent<RectTransform>();
            boardRT.sizeDelta = new Vector2(400, 600);

            // Ribbon
            GameObject ribbon = CreateImageNode("Ribbon", board.transform, FindSprite("BigRibbons 1"));
            RectTransform ribbonRT = ribbon.GetComponent<RectTransform>();
            ribbonRT.anchorMin = new Vector2(0.5f, 1f); ribbonRT.anchorMax = new Vector2(0.5f, 1f);
            ribbonRT.pivot = new Vector2(0.5f, 0.5f);
            ribbonRT.anchoredPosition = new Vector2(0, 0);
            ribbonRT.sizeDelta = new Vector2(350, 100);
            CreateTextNode("Title", ribbon.transform, "PAUSED", 40, Color.white).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // Bố cục dọc cho các nút bấm
            GameObject btnGroup = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            btnGroup.transform.SetParent(board.transform, false);
            btnGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
            btnGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 400);
            VerticalLayoutGroup vlg = btnGroup.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 30;

            Sprite btnSprite = FindSprite("TinySquareBlueButton");
            Button btnResume = CreateHitboxButton("ResumeBtn", btnGroup.transform, "Resume", btnSprite);
            Button btnOptions = CreateHitboxButton("OptionsBtn", btnGroup.transform, "Options", btnSprite);
            Button btnMenu = CreateHitboxButton("MainMenuBtn", btnGroup.transform, "Main Menu", btnSprite);

            // Gắn script quản lý và gỡ dây
            PauseUIController ctrl = root.AddComponent<PauseUIController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("resumeButton").objectReferenceValue = btnResume;
            so.FindProperty("optionsButton").objectReferenceValue = btnOptions;
            so.FindProperty("mainMenuButton").objectReferenceValue = btnMenu;
            so.FindProperty("pausePanel").objectReferenceValue = board; // Assign PausePanel
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Prefabs/UI/PauseUI.prefab");
            Object.DestroyImmediate(root);
        }
    }
}
