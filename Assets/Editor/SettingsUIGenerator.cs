using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AttoTheSheep.UI.Shared;
using System.IO;

namespace AttoTheSheep.Editor
{
    public class SettingsUIGenerator
    {
        [MenuItem("Tools/Generate Settings UI Prefab")]
        public static void GenerateSettingsPrefab()
        {
            EnsureFolderExists("Assets/Resources/Prefabs/UI");

            GameObject root = CreateRootPanel("SettingsPanelUI");

            // Nền mờ
            GameObject overlay = CreateImageNode("Overlay", root.transform, null, true);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;

            // Bảng chính
            GameObject boardObj = CreateImageNode("SettingsBoard", root.transform, FindSprite("WoodTable"));
            RectTransform boardRT = boardObj.GetComponent<RectTransform>();
            boardRT.sizeDelta = new Vector2(1120, 800);

            // Ribbon
            GameObject ribbon = CreateImageNode("Ribbon", boardObj.transform, FindSprite("Banner"));
            RectTransform ribbonRT = ribbon.GetComponent<RectTransform>();
            ribbonRT.anchorMin = new Vector2(0.5f, 1f); ribbonRT.anchorMax = new Vector2(0.5f, 1f);
            ribbonRT.anchoredPosition = new Vector2(0, -150);
            ribbonRT.sizeDelta = new Vector2(400, 100);
            CreateTextNode("Title", ribbon.transform, "SETTINGS", 40, Color.white, true).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);

            // Vertical Layout
            GameObject vLayoutObj = new GameObject("VerticalContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            vLayoutObj.transform.SetParent(boardObj.transform, false);
            RectTransform vRT = vLayoutObj.GetComponent<RectTransform>();
            vRT.anchoredPosition = new Vector2(0, -60);
            vRT.sizeDelta = new Vector2(620, 500); 
            
            VerticalLayoutGroup vlg = vLayoutObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 30; 
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;

            // Controller
            SettingsUIController ctrl = root.AddComponent<SettingsUIController>();

            // ROW 1: Username & Save
            GameObject row1 = CreateRow("Row_Username", vLayoutObj.transform);
            CreateTextNode("Label", row1.transform, "Username:", 35, Color.white, true).GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);
            
            GameObject usernameInput = CreateStandardInputField("UsernameInput", row1.transform);
            ctrl.usernameInput = usernameInput.GetComponent<TMP_InputField>();

            Button saveBtn = CreateHitboxButton("SaveBtn", row1.transform, "Save", FindSprite("TinySquareBlueButton"), 120, 50);
            ctrl.saveUsernameButton = saveBtn;

            // ROW 2: Sound Slider
            GameObject row2 = CreateRow("Row_Sound", vLayoutObj.transform);
            CreateTextNode("Label", row2.transform, "Sound:", 35, Color.white, true).GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);
            Slider soundSlider = CreateSlider("SoundSlider", row2.transform);
            ctrl.soundSlider = soundSlider;
            TextMeshProUGUI soundVal = CreateTextNode("ValueTxt", row2.transform, "100%", 30, Color.white, true);
            soundVal.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            ctrl.soundValueText = soundVal;

            // ROW 3: Music Slider
            GameObject row3 = CreateRow("Row_Music", vLayoutObj.transform);
            CreateTextNode("Label", row3.transform, "Music:", 35, Color.white, true).GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);
            Slider musicSlider = CreateSlider("MusicSlider", row3.transform);
            ctrl.musicSlider = musicSlider;
            TextMeshProUGUI musicVal = CreateTextNode("ValueTxt", row3.transform, "100%", 30, Color.white, true);
            musicVal.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            ctrl.musicValueText = musicVal;

            // ROW 4: Language
            GameObject row4 = CreateRow("Row_Language", vLayoutObj.transform);
            CreateTextNode("Label", row4.transform, "Language:", 35, Color.white, true).GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);
            GameObject langDropdown = CreateStandardDropdown("LanguageDropdown", row4.transform, new string[] { "English", "Vietnamese" });
            ctrl.languageDropdown = langDropdown.GetComponent<TMP_Dropdown>();

            // ROW 5: FPS
            GameObject row5 = CreateRow("Row_FPS", vLayoutObj.transform);
            CreateTextNode("Label", row5.transform, "Max FPS:", 35, Color.white, true).GetComponent<RectTransform>().sizeDelta = new Vector2(220, 50);
            GameObject fpsDropdown = CreateStandardDropdown("FPSDropdown", row5.transform, new string[] { "30", "60", "120", "Uncapped" });
            ctrl.fpsDropdown = fpsDropdown.GetComponent<TMP_Dropdown>();

            // BOTTOM: Back Button
            Button backBtn = CreateHitboxButton("BackToGameBtn", boardObj.transform, "BACK TO GAME", FindSprite("TinySquareRedButton"), 300, 80);
            RectTransform backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0); backRT.anchorMax = new Vector2(0.5f, 0);
            backRT.pivot = new Vector2(0.5f, 0);
            backRT.anchoredPosition = new Vector2(0, 50);

            // Save Prefab
            string path = "Assets/Resources/Prefabs/UI/SettingsPanelUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log($"<color=green>[SettingsUIGenerator]</color> Tự động tạo Prefab thành công tại: {path}");
        }

        private static GameObject CreateRow(string name, Transform parent)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft; 
            hlg.spacing = 20;
            hlg.childControlHeight = false; 
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            return row;
        }

        private static void EnsureFolderExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static Sprite FindSprite(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }

        private static GameObject CreateRootPanel(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
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

        private static TextMeshProUGUI CreateTextNode(string name, Transform parent, string text, int fontSize, Color color, bool outline = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            
            if (outline)
            {
                tmp.outlineWidth = 0.2f; 
                tmp.outlineColor = Color.black;
            }
            return tmp;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            GameObject go = DefaultControls.CreateSlider(uiResources);
            go.name = name;
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 30);
            
            Transform bgTransform = go.transform.Find("Background");
            if (bgTransform != null)
            {
                Image bgImg = bgTransform.GetComponent<Image>();
                bgImg.sprite = FindSprite("SmallBar_Base");
                bgImg.type = Image.Type.Simple;
                bgImg.color = Color.white;
            }

            Transform fillTransform = go.transform.Find("Fill Area/Fill");
            if (fillTransform != null)
            {
                Image fillImg = fillTransform.GetComponent<Image>();
                fillImg.sprite = FindSprite("SmallBar_Fill");
                fillImg.type = Image.Type.Simple;
            }
            
            Transform handle = go.transform.Find("Handle Slide Area/Handle");
            if (handle != null)
            {
                Image handleImg = handle.GetComponent<Image>();
                handleImg.sprite = FindSprite("TinySquareBlueButton"); 
                handleImg.type = Image.Type.Simple;
                handleImg.color = Color.white;
                handleImg.preserveAspect = true;
                handle.GetComponent<RectTransform>().sizeDelta = new Vector2(35, 35);
            }
            
            MakeInteractiveWithHover(go);
            return go.GetComponent<Slider>();
        }

        private static GameObject CreateStandardDropdown(string name, Transform parent, string[] options)
        {
            TMPro.TMP_DefaultControls.Resources uiResources = new TMPro.TMP_DefaultControls.Resources();
            uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            uiResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            uiResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");

            GameObject go = TMPro.TMP_DefaultControls.CreateDropdown(uiResources);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 60);

            Image bg = go.GetComponent<Image>();
            bg.sprite = FindSprite("BigBlueButton_Regular");
            bg.type = Image.Type.Simple;

            Transform label = go.transform.Find("Label");
            if (label != null)
            {
                TextMeshProUGUI txt = label.GetComponent<TextMeshProUGUI>();
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
            }

            Transform template = go.transform.Find("Template");
            if (template != null)
            {
                RectTransform templateRT = template.GetComponent<RectTransform>();
                templateRT.sizeDelta = new Vector2(0, 150); // Đảm bảo chiều rộng khớp parent
                Image tBg = template.GetComponent<Image>();
                tBg.sprite = FindSprite("RegularPaper");
                tBg.type = Image.Type.Sliced;
                tBg.color = Color.white;
            }

            Transform item = go.transform.Find("Template/Viewport/Content/Item");
            if (item != null)
            {
                item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50); // Tăng chiều cao mỗi mục
            }

            Transform itemBg = go.transform.Find("Template/Viewport/Content/Item/Item Background");
            if (itemBg != null)
            {
                Image itemBgImg = itemBg.GetComponent<Image>();
                itemBgImg.sprite = FindSprite("RegularPaper"); // Nền mục khi hover sẽ là giấy
                itemBgImg.type = Image.Type.Sliced;
                itemBgImg.color = Color.white;
            }

            Transform itemLabel = go.transform.Find("Template/Viewport/Content/Item/Item Label");
            if (itemLabel != null)
            {
                TextMeshProUGUI txt = itemLabel.GetComponent<TextMeshProUGUI>();
                txt.color = Color.black; // Chữ đen cho dễ đọc trên nền giấy
                txt.fontSize = 28; // Tăng size chữ
                txt.alignment = TextAlignmentOptions.Center;
            }

            Transform arrow = go.transform.Find("Arrow");
            if (arrow != null)
            {
                Image arrowImg = arrow.GetComponent<Image>();
                arrowImg.color = Color.white;
                arrowImg.raycastTarget = false;
                arrowImg.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            }

            TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
            
            MakeInteractiveWithHover(go);
            return go;
        }

        private static GameObject CreateStandardInputField(string name, Transform parent)
        {
            TMPro.TMP_DefaultControls.Resources uiResources = new TMPro.TMP_DefaultControls.Resources();
            uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            uiResources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

            GameObject go = TMPro.TMP_DefaultControls.CreateInputField(uiResources);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 60);

            Image bg = go.GetComponent<Image>();
            bg.sprite = FindSprite("BigBlueButton_Regular");
            bg.type = Image.Type.Simple;

            TMP_InputField input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 15;
            if (input.textComponent != null) {
                input.textComponent.color = Color.white;
                input.textComponent.alignment = TextAlignmentOptions.Center;
            }
            if (input.placeholder != null) {
                input.placeholder.color = new Color(1, 1, 1, 0.5f);
                input.placeholder.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            }

            MakeInteractiveWithHover(go);
            return go;
        }

        private static void MakeInteractiveWithHover(GameObject go)
        {
            Image bg = go.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = false;

            Transform handle = go.transform.Find("Handle Slide Area/Handle");
            if (handle != null && handle.GetComponent<Image>() != null) handle.GetComponent<Image>().raycastTarget = false;
            
            Transform sliderBg = go.transform.Find("Background");
            if (sliderBg != null && sliderBg.GetComponent<Image>() != null) sliderBg.GetComponent<Image>().raycastTarget = false;

            // Chỉ tắt raycast của Label chính, Text của InputField, KHÔNG tắt của Template (sẽ làm hỏng Dropdown List)
            TextMeshProUGUI[] tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach(var tmp in tmps) 
            {
                if (tmp.gameObject.name == "Label" || tmp.gameObject.name == "Placeholder" || tmp.gameObject.name == "Text")
                    tmp.raycastTarget = false;
            }

            if (go.GetComponent<TMP_Dropdown>() != null || go.GetComponent<TMP_InputField>() != null)
            {
                GameObject hitboxObj = new GameObject("Hitbox", typeof(RectTransform), typeof(Image), typeof(HoverCursor));
                hitboxObj.transform.SetParent(go.transform, false);
                hitboxObj.transform.SetAsFirstSibling(); // Đưa hitbox xuống lớp dưới cùng để không che Template
                RectTransform hitboxRT = hitboxObj.GetComponent<RectTransform>();
                hitboxRT.anchorMin = Vector2.zero; hitboxRT.anchorMax = Vector2.one;
                hitboxRT.offsetMin = Vector2.zero; hitboxRT.offsetMax = Vector2.zero;
                
                Image hitboxImg = hitboxObj.GetComponent<Image>();
                hitboxImg.color = new Color(0, 0, 0, 0);
                hitboxImg.raycastTarget = true;
                // Tuyệt đối KHÔNG gán targetGraphic = hitboxImg ở đây, vì sẽ làm mất hiệu ứng hover đổi màu của hình nền chính!
            }
            else if (go.GetComponent<Slider>() != null)
            {
                if (handle != null) {
                    GameObject hitboxObj = new GameObject("Hitbox", typeof(RectTransform), typeof(Image), typeof(HoverCursor));
                    hitboxObj.transform.SetParent(handle, false);
                    hitboxObj.transform.SetAsFirstSibling();
                    RectTransform hitboxRT = hitboxObj.GetComponent<RectTransform>();
                    hitboxRT.anchorMin = Vector2.zero; hitboxRT.anchorMax = Vector2.one;
                    hitboxRT.offsetMin = new Vector2(-10, -10); hitboxRT.offsetMax = new Vector2(10, 10);
                    
                    Image hitboxImg = hitboxObj.GetComponent<Image>();
                    hitboxImg.color = new Color(0, 0, 0, 0);
                    hitboxImg.raycastTarget = true;
                }
            }
        }

        private static Button CreateHitboxButton(string name, Transform parent, string btnText, Sprite bgSprite, float width, float height)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = bgSprite;
            if (bgSprite != null) img.type = Image.Type.Sliced;
            img.raycastTarget = false; 

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject hitboxObj = new GameObject("Hitbox", typeof(RectTransform), typeof(Image), typeof(HoverCursor));
            hitboxObj.transform.SetParent(btnObj.transform, false);
            RectTransform hitboxRT = hitboxObj.GetComponent<RectTransform>();
            hitboxRT.anchorMin = Vector2.zero; hitboxRT.anchorMax = Vector2.one;
            hitboxRT.offsetMin = new Vector2(10, 10); hitboxRT.offsetMax = new Vector2(-10, -10); 
            
            Image hitboxImg = hitboxObj.GetComponent<Image>();
            hitboxImg.color = new Color(0, 0, 0, 0);
            hitboxImg.raycastTarget = true;

            TextMeshProUGUI tmp = CreateTextNode("Text", btnObj.transform, btnText, 25, Color.white, true);
            RectTransform textRT = tmp.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
