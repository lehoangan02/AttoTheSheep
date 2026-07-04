#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using AttoTheSheep.UI.ShopAndInventory;

namespace AttoTheSheep.Editor
{
    public class ShopInventoryUIBuilder : EditorWindow
    {
        [MenuItem("Tools/Build Shop & Inventory UI (TinySwords)")]
        public static void GenerateUI()
        {
            // Tìm Canvas có sẵn trong Scene (trừ Canvas của Transition)
            Canvas existingCanvas = null;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach(var c in canvases)
            {
                if (c.name == "Canvas" || c.name == "PlayerUI") 
                {
                    existingCanvas = c;
                    break;
                }
            }

            GameObject uiRoot = new GameObject("ShopInventoryUI");
            
            if (existingCanvas != null)
            {
                uiRoot.transform.SetParent(existingCanvas.transform, false);
                // Mở rộng toàn màn hình
                RectTransform rt = uiRoot.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.SetAsLastSibling(); // Lên trên cùng
            }
            else
            {
                Canvas canvas = uiRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                uiRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                uiRoot.AddComponent<GraphicRaycaster>();
            }

            // 1. Tạo Managers & Root
            GameObject managers = new GameObject("Managers");
            managers.transform.SetParent(uiRoot.transform);
            managers.AddComponent<InventoryManager>();
            ShopManager shopManager = managers.AddComponent<ShopManager>();
            
            ShopInventoryRoot rootScript = uiRoot.AddComponent<ShopInventoryRoot>();
            
            // Tìm các Item có sẵn để gán vào Shop
            string[] itemGuids = AssetDatabase.FindAssets("t:ActionItem");
            foreach(string guid in itemGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionItem item = AssetDatabase.LoadAssetAtPath<ActionItem>(path);
                if (item != null) shopManager.shopItems.Add(item);
            }

            // Load Sprites từ TinySwords
            Sprite woodTable = LoadSprite("WoodTable");
            Sprite slotSprite = LoadSprite("WoodTable_Slots", true); // Cần Readable để check Hitbox
            Sprite btnSprite = LoadSprite("BigBlueButton_Regular", true); // Cần Readable để check Hitbox
            Sprite closeBtnSprite = LoadSprite("Icon_05"); // Dấu X
            Sprite coinSprite = LoadSprite("Icon_04"); // Đồng vàng
            Sprite ribbonSprite = LoadSprite("BigRibbons 1"); // Banner
            TMP_FontAsset fontAsset = LoadFont("Skagwae Regular SDF"); // Đổi Font
            Texture2D defCursor = LoadTexture("Cursor_01");
            Texture2D hovCursor = LoadTexture("Cursor_02");

            // 2. Tạo Shop Panel
            GameObject shopPanelBlocker = CreatePanel(uiRoot.transform, "ShopPanel", woodTable, new Vector2(1000, 700), defCursor, hovCursor);
            Transform shopPanel = shopPanelBlocker.transform.GetChild(0);
            CreateShopUI(shopPanel, slotSprite, btnSprite, coinSprite, ribbonSprite, shopManager, fontAsset, defCursor, hovCursor);
            shopPanelBlocker.SetActive(false);

            // 3. Tạo Inventory Panel
            GameObject invPanelBlocker = CreatePanel(uiRoot.transform, "InventoryPanel", woodTable, new Vector2(1200, 700), defCursor, hovCursor);
            Transform invPanel = invPanelBlocker.transform.GetChild(0);
            CreateInventoryUI(invPanel, slotSprite, btnSprite, coinSprite, ribbonSprite, fontAsset, defCursor, hovCursor);
            invPanelBlocker.SetActive(false);

            // Gán Panel cho RootScript (Gán Root Blocker để khi bật tắt sẽ bật tắt cả blocker)
            rootScript.shopPanel = shopPanelBlocker;
            rootScript.inventoryPanel = invPanelBlocker;

            // Lưu thành Prefab
            string prefabPath = "Assets/Resources/Prefabs/UI/ShopInventoryUI.prefab";
            if (!System.IO.Directory.Exists("Assets/Resources/Prefabs/UI"))
                System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
            
            PrefabUtility.SaveAsPrefabAsset(uiRoot, prefabPath);
            Debug.Log($"[UIBuilder] Đã tạo thành công UI tại {prefabPath}");
        }

        private static Sprite LoadSprite(string name, bool makeReadable = false)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (makeReadable)
                {
                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static TMP_FontAsset LoadFont(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:TMP_FontAsset");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }

        private static Texture2D LoadTexture(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private static GameObject CreatePanel(Transform parent, string name, Sprite bg, Vector2 size, Texture2D defCursor, Texture2D hovCursor)
        {
            // Blocker (chắn click ra ngoài)
            GameObject blocker = new GameObject(name + "_Root");
            blocker.transform.SetParent(parent, false);
            RectTransform bRect = blocker.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            Image bImg = blocker.AddComponent<Image>();
            bImg.color = new Color(0, 0, 0, 0.7f); // Đen mờ 70%
            bImg.raycastTarget = true; // Chặn mọi cú click xuyên qua
            // Blockers shouldn't trigger hover cursor, but just in case, no HoverCursor component.

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(blocker.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            Image img = panel.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;
            
            // Nút Close
            GameObject closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(panel.transform, false);
            RectTransform cRect = closeBtn.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.anchoredPosition = new Vector2(-15, -15); // Lệch vào trong 1 xíu
            cRect.sizeDelta = new Vector2(60, 60);
            closeBtn.AddComponent<Image>().sprite = LoadSprite("Icon_05");
            Button cBtn = closeBtn.AddComponent<Button>();
            // Gắn tạm logic Editor, lúc Runtime sẽ bị xóa
            cBtn.onClick.AddListener(() => blocker.SetActive(false));
            
            HoverCursor hc = closeBtn.AddComponent<HoverCursor>();
            var hso = new SerializedObject(hc);
            hso.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hso.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hso.ApplyModifiedProperties();

            return blocker;
        }

        private static void CreateBannerTitle(Transform parent, string text, Sprite ribbonSprite, TMP_FontAsset font)
        {
            // Banner Background
            GameObject banner = new GameObject("TitleBanner");
            banner.transform.SetParent(parent, false);
            RectTransform bRect = banner.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 1f); // Neo ở chính giữa cạnh trên
            bRect.anchorMax = new Vector2(0.5f, 1f);
            bRect.anchoredPosition = new Vector2(0, 30); // Lên trên mép gỗ một xíu
            bRect.sizeDelta = new Vector2(400, 120);
            Image bImg = banner.AddComponent<Image>();
            bImg.sprite = ribbonSprite;
            // Dùng preserve aspect hoặc type tùy sprite
            bImg.type = Image.Type.Sliced;

            // Tiêu đề
            GameObject title = new GameObject("TitleText");
            title.transform.SetParent(banner.transform, false);
            RectTransform tRect = title.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(50, 30); // Padding left, bottom (Tránh tràn viền ribbon)
            tRect.offsetMax = new Vector2(-50, -30); // Padding right, top
            
            TextMeshProUGUI tmp = title.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSizeMax = 60;
            tmp.fontSizeMin = 20;
            tmp.enableAutoSizing = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white; // Chữ trắng nổi bật trên nền đỏ/xanh của Banner
            tmp.fontStyle = FontStyles.Bold;
            if (font != null) tmp.font = font;

            // Thêm viền đen cho chữ thêm nổi bật
            Outline outline = title.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
        }

        private static void CreateShopUI(Transform shopPanel, Sprite slotSprite, Sprite btnSprite, Sprite coinSprite, Sprite ribbonSprite, ShopManager sm, TMP_FontAsset font, Texture2D defCursor, Texture2D hovCursor)
        {
            // Banner Tiêu Đề
            CreateBannerTitle(shopPanel, "SHOP", ribbonSprite, font);

            ShopUI suiScript = shopPanel.gameObject.AddComponent<ShopUI>();
            var rootSo = new SerializedObject(suiScript);

            // Left Grid
            GameObject grid = new GameObject("ItemsGrid");
            grid.transform.SetParent(shopPanel, false);
            // Lưới hiển thị căn giữa, thiết kế dành riêng cho đúng 4 Item
            RectTransform gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.2f);
            gridRect.anchorMax = new Vector2(0.95f, 0.8f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            
            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(190, 260); // Phóng to 1 xíu
            layout.spacing = new Vector2(30, 0); // Cách xa nhau ra 1 xíu
            layout.childAlignment = TextAnchor.MiddleCenter; // Canh đúng chính giữa màn hình

            // Generate Slots
            foreach(var item in sm.shopItems)
            {
                GameObject slot = new GameObject("ShopSlot");
                slot.transform.SetParent(grid.transform, false);
                slot.AddComponent<Image>().sprite = slotSprite;

                // Icon
                GameObject icon = new GameObject("Icon");
                icon.transform.SetParent(slot.transform, false);
                RectTransform iRect = icon.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0, 30);
                iRect.sizeDelta = new Vector2(100, 100); 
                icon.AddComponent<Image>().sprite = item.icon;

                // Buy Btn
                GameObject btn = new GameObject("BuyBtn");
                btn.transform.SetParent(slot.transform, false);
                RectTransform bRect = btn.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0, -70);
                bRect.sizeDelta = new Vector2(140, 50); // Rộng ra tí cho vừa chữ
                Image btnImg = btn.AddComponent<Image>();
                btnImg.sprite = btnSprite;
                btnImg.type = Image.Type.Sliced;
                btnImg.alphaHitTestMinimumThreshold = 0.1f; // Hitbox khớp hình nút
                
                GameObject priceTxt = new GameObject("PriceTxt");
                priceTxt.transform.SetParent(btn.transform, false);
                RectTransform ptRect = priceTxt.AddComponent<RectTransform>();
                ptRect.anchorMin = Vector2.zero;
                ptRect.anchorMax = Vector2.one;
                ptRect.offsetMin = new Vector2(10, 5); // Thụt vào tránh viền nút
                ptRect.offsetMax = new Vector2(-10, -5);
                
                TextMeshProUGUI pTmp = priceTxt.AddComponent<TextMeshProUGUI>();
                pTmp.text = item.price + "$";
                pTmp.color = Color.white; // Chữ trắng trên nền xanh/đỏ của nút
                pTmp.alignment = TextAlignmentOptions.Center;
                pTmp.fontSizeMax = 40;
                pTmp.fontSizeMin = 10;
                pTmp.enableAutoSizing = true;
                if (font != null) pTmp.font = font;

                Outline pOutline = priceTxt.AddComponent<Outline>();
                pOutline.effectColor = Color.black;
                pOutline.effectDistance = new Vector2(1, -1);

                // Badge
                GameObject badge = new GameObject("Badge");
                badge.transform.SetParent(slot.transform, false);
                RectTransform badgeRect = badge.AddComponent<RectTransform>();
                badgeRect.anchoredPosition = new Vector2(80, 100);
                badgeRect.sizeDelta = new Vector2(40, 40);
                badge.AddComponent<Image>().color = Color.red; 
                
                GameObject bTxt = new GameObject("Count");
                bTxt.transform.SetParent(badge.transform, false);
                RectTransform btRect = bTxt.AddComponent<RectTransform>();
                btRect.sizeDelta = new Vector2(30, 30);
                TextMeshProUGUI bTmp = bTxt.AddComponent<TextMeshProUGUI>();
                bTmp.text = "1";
                bTmp.alignment = TextAlignmentOptions.Center;
                bTmp.color = Color.white;
                if (font != null) bTmp.font = font;

                // Setup Script
                ShopSlotUI sui = slot.AddComponent<ShopSlotUI>();
                var so = new SerializedObject(sui);
                so.FindProperty("iconImage").objectReferenceValue = icon.GetComponent<Image>();
                so.FindProperty("priceText").objectReferenceValue = pTmp;
                
                Button bBtn = btn.AddComponent<Button>();
                so.FindProperty("buyButton").objectReferenceValue = bBtn;
                
                Button sBtn = slot.AddComponent<Button>();
                so.FindProperty("selectButton").objectReferenceValue = sBtn;
                
                Image selImg = slot.GetComponent<Image>();
                selImg.alphaHitTestMinimumThreshold = 0.1f;
                
                HoverCursor hcBtn = btn.AddComponent<HoverCursor>();
                var hso = new SerializedObject(hcBtn);
                hso.FindProperty("defaultCursor").objectReferenceValue = defCursor;
                hso.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
                hso.ApplyModifiedProperties();
                
                HoverCursor hcSlot = slot.AddComponent<HoverCursor>();
                var hsso = new SerializedObject(hcSlot);
                hsso.FindProperty("defaultCursor").objectReferenceValue = defCursor;
                hsso.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
                hsso.ApplyModifiedProperties();

                so.FindProperty("badgeRoot").objectReferenceValue = badge;
                so.FindProperty("badgeCountText").objectReferenceValue = bTmp;
                so.FindProperty("_currentItem").objectReferenceValue = item;
                so.FindProperty("_shopUI").objectReferenceValue = suiScript;
                so.ApplyModifiedProperties();
                
                sui.Setup(item, suiScript); // Update UI preview in Editor
            }

            // Central Popup (Details)
            GameObject detailPanel = new GameObject("PopupPanel");
            detailPanel.transform.SetParent(shopPanel, false);
            RectTransform dRect = detailPanel.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.anchoredPosition = Vector2.zero;
            dRect.sizeDelta = new Vector2(400, 600);
            
            // Xài luôn nền gỗ cho popup
            Image popImg = detailPanel.AddComponent<Image>();
            popImg.sprite = slotSprite;
            popImg.type = Image.Type.Sliced;
            popImg.raycastTarget = true; // Chặn click xuyên qua popup
            
            rootSo.FindProperty("popupRoot").objectReferenceValue = detailPanel;

            // Close Button cho Popup
            GameObject popCloseBtn = new GameObject("CloseBtn");
            popCloseBtn.transform.SetParent(detailPanel.transform, false);
            RectTransform cRect = popCloseBtn.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.anchoredPosition = new Vector2(-10, -10);
            cRect.sizeDelta = new Vector2(50, 50);
            popCloseBtn.AddComponent<Image>().sprite = LoadSprite("Icon_05");
            Button cBtn = popCloseBtn.AddComponent<Button>();
            
            HoverCursor hcPopClose = popCloseBtn.AddComponent<HoverCursor>();
            var hsoPopClose = new SerializedObject(hcPopClose);
            hsoPopClose.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoPopClose.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoPopClose.ApplyModifiedProperties();
            
            rootSo.FindProperty("closeButton").objectReferenceValue = cBtn;
            rootSo.ApplyModifiedProperties();

            GameObject dIcon = new GameObject("DetailIcon");
            dIcon.transform.SetParent(detailPanel.transform, false);
            RectTransform diRect = dIcon.AddComponent<RectTransform>();
            diRect.anchoredPosition = new Vector2(0, 100);
            diRect.sizeDelta = new Vector2(150, 150);
            rootSo.FindProperty("detailIcon").objectReferenceValue = dIcon.AddComponent<Image>();

            GameObject dName = new GameObject("DetailName");
            dName.transform.SetParent(detailPanel.transform, false);
            RectTransform dnRect = dName.AddComponent<RectTransform>();
            dnRect.anchoredPosition = new Vector2(0, 0);
            dnRect.sizeDelta = new Vector2(300, 50);
            TextMeshProUGUI dnTmp = dName.AddComponent<TextMeshProUGUI>();
            dnTmp.alignment = TextAlignmentOptions.Center;
            dnTmp.fontSize = 40;
            dnTmp.color = Color.yellow;
            if (font != null) dnTmp.font = font;
            dName.AddComponent<Outline>().effectColor = Color.black;
            rootSo.FindProperty("detailName").objectReferenceValue = dnTmp;

            GameObject dDesc = new GameObject("DetailDesc");
            dDesc.transform.SetParent(detailPanel.transform, false);
            RectTransform ddRect = dDesc.AddComponent<RectTransform>();
            ddRect.anchoredPosition = new Vector2(0, -80);
            ddRect.sizeDelta = new Vector2(300, 100);
            TextMeshProUGUI ddTmp = dDesc.AddComponent<TextMeshProUGUI>();
            ddTmp.alignment = TextAlignmentOptions.TopLeft;
            ddTmp.enableWordWrapping = true;
            ddTmp.color = Color.white;
            if (font != null) ddTmp.font = font;
            dDesc.AddComponent<Outline>().effectColor = Color.black;
            rootSo.FindProperty("detailDesc").objectReferenceValue = ddTmp;

            GameObject dPrice = new GameObject("DetailPrice");
            dPrice.transform.SetParent(detailPanel.transform, false);
            RectTransform dpRect = dPrice.AddComponent<RectTransform>();
            dpRect.anchoredPosition = new Vector2(0, -140);
            dpRect.sizeDelta = new Vector2(300, 50);
            TextMeshProUGUI dpTmp = dPrice.AddComponent<TextMeshProUGUI>();
            dpTmp.alignment = TextAlignmentOptions.Center;
            dpTmp.color = Color.yellow;
            dpTmp.fontSize = 35;
            if (font != null) dpTmp.font = font;
            dPrice.AddComponent<Outline>().effectColor = Color.black;
            rootSo.FindProperty("detailPrice").objectReferenceValue = dpTmp;

            // Buy Button in Detail Panel
            GameObject buyBtn = new GameObject("BuyBtn");
            buyBtn.transform.SetParent(detailPanel.transform, false);
            RectTransform buyRect = buyBtn.AddComponent<RectTransform>();
            buyRect.anchoredPosition = new Vector2(0, -210);
            buyRect.sizeDelta = new Vector2(160, 60);
            
            buyBtn.AddComponent<Image>().sprite = btnSprite;
            buyBtn.GetComponent<Image>().type = Image.Type.Sliced;
            buyBtn.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
            
            Button popBuyBtn = buyBtn.AddComponent<Button>();
            HoverCursor hcBuy = buyBtn.AddComponent<HoverCursor>();
            var hsoBuy = new SerializedObject(hcBuy);
            hsoBuy.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoBuy.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoBuy.ApplyModifiedProperties();

            GameObject buyTxt = new GameObject("Txt");
            buyTxt.transform.SetParent(buyBtn.transform, false);
            RectTransform bttRect = buyTxt.AddComponent<RectTransform>();
            bttRect.anchorMin = Vector2.zero;
            bttRect.anchorMax = Vector2.one;
            bttRect.offsetMin = new Vector2(10, 5);
            bttRect.offsetMax = new Vector2(-10, -5);
            TextMeshProUGUI buyTmp = buyTxt.AddComponent<TextMeshProUGUI>();
            buyTmp.text = "BUY";
            buyTmp.color = Color.white;
            buyTmp.alignment = TextAlignmentOptions.Center;
            buyTmp.fontSizeMax = 45;
            buyTmp.fontSizeMin = 20;
            buyTmp.enableAutoSizing = true;
            if (font != null) buyTmp.font = font;
            buyTxt.AddComponent<Outline>().effectColor = Color.black;
            
            rootSo.FindProperty("buyButton").objectReferenceValue = popBuyBtn;
            rootSo.ApplyModifiedProperties();
        }

        private static void CreateInventoryUI(Transform invPanel, Sprite slotSprite, Sprite btnSprite, Sprite coinSprite, Sprite ribbonSprite, TMP_FontAsset font, Texture2D defCursor, Texture2D hovCursor)
        {
            // Banner Tiêu Đề
            CreateBannerTitle(invPanel, "INVENTORY", ribbonSprite, font);

            InventoryUI iui = invPanel.gameObject.AddComponent<InventoryUI>();
            var so = new SerializedObject(iui);

            // Left Grid
            GameObject grid = new GameObject("ItemsGrid");
            grid.transform.SetParent(invPanel, false);
            RectTransform gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.2f);
            gridRect.anchorMax = new Vector2(0.55f, 0.85f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;
            
            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(85, 85); // Nhỏ lại để vừa 20 ô không bị tràn
            layout.spacing = new Vector2(10, 10);
            so.FindProperty("slotsParent").objectReferenceValue = grid.transform;

            // Slot Prefab
            GameObject slotPref = new GameObject("InvSlotPrefab");
            slotPref.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            slotPref.AddComponent<Image>().sprite = slotSprite;
            
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slotPref.transform, false);
            icon.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            Image icImg = icon.AddComponent<Image>();
            
            GameObject count = new GameObject("CountTxt");
            count.transform.SetParent(slotPref.transform, false);
            RectTransform cRect = count.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1, 0);
            cRect.anchorMax = new Vector2(1, 0);
            cRect.pivot = new Vector2(1, 0);
            cRect.anchoredPosition = new Vector2(-5, 5);
            cRect.sizeDelta = new Vector2(40, 40);
            TextMeshProUGUI cTmp = count.AddComponent<TextMeshProUGUI>();
            cTmp.text = "x1";
            cTmp.color = Color.white;
            cTmp.alignment = TextAlignmentOptions.BottomRight;
            cTmp.fontSizeMax = 30;
            cTmp.fontSizeMin = 10;
            cTmp.enableAutoSizing = true;
            if (font != null) cTmp.font = font;
            count.AddComponent<Outline>().effectColor = Color.black;
            
            InventorySlotUI slotScript = slotPref.AddComponent<InventorySlotUI>();
            var sso = new SerializedObject(slotScript);
            sso.FindProperty("iconImage").objectReferenceValue = icImg;
            sso.FindProperty("countText").objectReferenceValue = cTmp;
            
            Button selBtn = slotPref.AddComponent<Button>();
            Image selImg = slotPref.GetComponent<Image>();
            selImg.alphaHitTestMinimumThreshold = 0.1f;
            
            HoverCursor hcSlot = slotPref.AddComponent<HoverCursor>();
            var hsoSlot = new SerializedObject(hcSlot);
            hsoSlot.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoSlot.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoSlot.ApplyModifiedProperties();

            sso.FindProperty("selectButton").objectReferenceValue = selBtn;
            sso.ApplyModifiedProperties();

            if (!System.IO.Directory.Exists("Assets/Resources/Prefabs/UI"))
                System.IO.Directory.CreateDirectory("Assets/Resources/Prefabs/UI");
            GameObject savedSlot = PrefabUtility.SaveAsPrefabAsset(slotPref, "Assets/Resources/Prefabs/UI/InvSlotPrefab.prefab");
            DestroyImmediate(slotPref);
            so.FindProperty("slotPrefab").objectReferenceValue = savedSlot;

            // Right Panel (Details Container)
            GameObject detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(invPanel, false);
            RectTransform dRect = detailPanel.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.57f, 0.2f); // Căn lề dưới bằng với Grid
            dRect.anchorMax = new Vector2(0.95f, 0.85f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;
            detailPanel.AddComponent<Image>().color = new Color(0,0,0,0.5f);

            // Placeholder View
            GameObject placeholderView = new GameObject("Placeholder");
            placeholderView.transform.SetParent(detailPanel.transform, false);
            RectTransform pRect = placeholderView.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;
            TextMeshProUGUI pTxt = placeholderView.AddComponent<TextMeshProUGUI>();
            pTxt.text = "Select an item to view details";
            pTxt.alignment = TextAlignmentOptions.Center;
            pTxt.color = new Color(1, 1, 1, 0.5f);
            pTxt.fontSize = 24;
            if (font != null) pTxt.font = font;

            so.FindProperty("placeholderView").objectReferenceValue = placeholderView;

            // Detail View
            GameObject detailView = new GameObject("DetailView");
            detailView.transform.SetParent(detailPanel.transform, false);
            RectTransform dvRect = detailView.AddComponent<RectTransform>();
            dvRect.anchorMin = Vector2.zero;
            dvRect.anchorMax = Vector2.one;
            dvRect.offsetMin = Vector2.zero;
            dvRect.offsetMax = Vector2.zero;

            so.FindProperty("detailView").objectReferenceValue = detailView;

            GameObject dIcon = new GameObject("DetailIcon");
            dIcon.transform.SetParent(detailView.transform, false);
            RectTransform diRect = dIcon.AddComponent<RectTransform>();
            diRect.anchoredPosition = new Vector2(0, 80);
            diRect.sizeDelta = new Vector2(150, 150);
            so.FindProperty("detailIcon").objectReferenceValue = dIcon.AddComponent<Image>();

            GameObject dName = new GameObject("DetailName");
            dName.transform.SetParent(detailView.transform, false);
            RectTransform dnRect = dName.AddComponent<RectTransform>();
            dnRect.anchoredPosition = new Vector2(0, -20);
            dnRect.sizeDelta = new Vector2(300, 40);
            TextMeshProUGUI dnTmp = dName.AddComponent<TextMeshProUGUI>();
            dnTmp.alignment = TextAlignmentOptions.Center;
            dnTmp.fontSize = 35;
            dnTmp.color = Color.yellow; 
            if (font != null) dnTmp.font = font;
            so.FindProperty("detailName").objectReferenceValue = dnTmp;

            GameObject dDesc = new GameObject("DetailDesc");
            dDesc.transform.SetParent(detailView.transform, false);
            RectTransform ddRect = dDesc.AddComponent<RectTransform>();
            ddRect.anchoredPosition = new Vector2(0, -90);
            ddRect.sizeDelta = new Vector2(300, 100);
            TextMeshProUGUI ddTmp = dDesc.AddComponent<TextMeshProUGUI>();
            ddTmp.alignment = TextAlignmentOptions.Top;
            ddTmp.enableWordWrapping = true;
            ddTmp.color = Color.white;
            if (font != null) ddTmp.font = font;
            so.FindProperty("detailDesc").objectReferenceValue = ddTmp;

            // Use / Drop Buttons (Inside Detail View)
            GameObject useBtnObj = new GameObject("UseBtn");
            useBtnObj.transform.SetParent(detailView.transform, false);
            RectTransform useRect = useBtnObj.AddComponent<RectTransform>();
            useRect.anchoredPosition = new Vector2(-75, -170);
            useRect.sizeDelta = new Vector2(130, 45);
            useBtnObj.AddComponent<Image>().sprite = btnSprite;
            useBtnObj.GetComponent<Image>().type = Image.Type.Sliced;
            useBtnObj.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
            Button uBtn = useBtnObj.AddComponent<Button>();
            
            GameObject useTxtObj = new GameObject("Txt");
            useTxtObj.transform.SetParent(useBtnObj.transform, false);
            RectTransform useTxtRect = useTxtObj.AddComponent<RectTransform>();
            useTxtRect.anchorMin = Vector2.zero;
            useTxtRect.anchorMax = Vector2.one;
            useTxtRect.offsetMin = new Vector2(5, 5);
            useTxtRect.offsetMax = new Vector2(-5, -5);
            TextMeshProUGUI useTmp = useTxtObj.AddComponent<TextMeshProUGUI>();
            useTmp.text = "Use";
            useTmp.color = Color.white;
            useTmp.alignment = TextAlignmentOptions.Center;
            useTmp.enableAutoSizing = true;
            if (font != null) useTmp.font = font;
            useTxtObj.AddComponent<Outline>().effectColor = Color.black;
            
            HoverCursor hcUse = useBtnObj.AddComponent<HoverCursor>();
            var hsoUse = new SerializedObject(hcUse);
            hsoUse.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoUse.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoUse.ApplyModifiedProperties();
            
            so.FindProperty("useButton").objectReferenceValue = uBtn;

            GameObject dropBtnObj = new GameObject("DropBtn");
            dropBtnObj.transform.SetParent(detailView.transform, false);
            RectTransform dropRect = dropBtnObj.AddComponent<RectTransform>();
            dropRect.anchoredPosition = new Vector2(75, -170);
            dropRect.sizeDelta = new Vector2(130, 45);
            dropBtnObj.AddComponent<Image>().sprite = btnSprite;
            dropBtnObj.GetComponent<Image>().type = Image.Type.Sliced;
            dropBtnObj.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
            Button dBtn = dropBtnObj.AddComponent<Button>();
            
            GameObject dropTxtObj = new GameObject("Txt");
            dropTxtObj.transform.SetParent(dropBtnObj.transform, false);
            RectTransform dropTxtRect = dropTxtObj.AddComponent<RectTransform>();
            dropTxtRect.anchorMin = Vector2.zero;
            dropTxtRect.anchorMax = Vector2.one;
            dropTxtRect.offsetMin = new Vector2(5, 5);
            dropTxtRect.offsetMax = new Vector2(-5, -5);
            TextMeshProUGUI dropTmp = dropTxtObj.AddComponent<TextMeshProUGUI>();
            dropTmp.text = "Drop";
            dropTmp.color = Color.white;
            dropTmp.alignment = TextAlignmentOptions.Center;
            dropTmp.enableAutoSizing = true;
            if (font != null) dropTmp.font = font;
            dropTxtObj.AddComponent<Outline>().effectColor = Color.black;
            
            HoverCursor hcDrop = dropBtnObj.AddComponent<HoverCursor>();
            var hsoDrop = new SerializedObject(hcDrop);
            hsoDrop.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoDrop.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoDrop.ApplyModifiedProperties();

            so.FindProperty("dropButton").objectReferenceValue = dBtn;

            // Upload / Fetch Buttons (Moved to bottom left outside Detail Panel)
            GameObject upBtnObj = new GameObject("UploadBtn");
            upBtnObj.transform.SetParent(invPanel, false);
            RectTransform upRect = upBtnObj.AddComponent<RectTransform>();
            upRect.anchorMin = new Vector2(0.3f, 0.1f);
            upRect.anchorMax = new Vector2(0.3f, 0.1f);
            upRect.anchoredPosition = Vector2.zero;
            upRect.sizeDelta = new Vector2(130, 45);
            upBtnObj.AddComponent<Image>().sprite = btnSprite;
            upBtnObj.GetComponent<Image>().type = Image.Type.Sliced;
            upBtnObj.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
            
            Button upBtn = upBtnObj.AddComponent<Button>();
            HoverCursor hcUp = upBtnObj.AddComponent<HoverCursor>();
            var hsoUp = new SerializedObject(hcUp);
            hsoUp.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoUp.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoUp.ApplyModifiedProperties();
            
            GameObject upTxt = new GameObject("Txt");
            upTxt.transform.SetParent(upBtnObj.transform, false);
            RectTransform utRect = upTxt.AddComponent<RectTransform>();
            utRect.anchorMin = Vector2.zero;
            utRect.anchorMax = Vector2.one;
            utRect.offsetMin = new Vector2(10, 5);
            utRect.offsetMax = new Vector2(-10, -5);
            TextMeshProUGUI uTmp = upTxt.AddComponent<TextMeshProUGUI>();
            uTmp.text = "Upload";
            uTmp.color = Color.white;
            uTmp.alignment = TextAlignmentOptions.Center;
            uTmp.fontSizeMax = 35;
            uTmp.fontSizeMin = 10;
            uTmp.enableAutoSizing = true;
            if (font != null) uTmp.font = font;
            upTxt.AddComponent<Outline>().effectColor = Color.black;
            so.FindProperty("uploadButton").objectReferenceValue = upBtn;

            GameObject ftBtnObj = new GameObject("FetchBtn");
            ftBtnObj.transform.SetParent(invPanel, false);
            RectTransform ftRect = ftBtnObj.AddComponent<RectTransform>();
            ftRect.anchorMin = new Vector2(0.45f, 0.1f);
            ftRect.anchorMax = new Vector2(0.45f, 0.1f);
            ftRect.anchoredPosition = Vector2.zero;
            ftRect.sizeDelta = new Vector2(130, 45);
            
            ftBtnObj.AddComponent<Image>().sprite = btnSprite;
            ftBtnObj.GetComponent<Image>().type = Image.Type.Sliced;
            ftBtnObj.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
            
            Button fBtn = ftBtnObj.AddComponent<Button>();
            HoverCursor hcFt = ftBtnObj.AddComponent<HoverCursor>();
            var hsoFt = new SerializedObject(hcFt);
            hsoFt.FindProperty("defaultCursor").objectReferenceValue = defCursor;
            hsoFt.FindProperty("hoverCursor").objectReferenceValue = hovCursor;
            hsoFt.ApplyModifiedProperties();

            GameObject ftTxt = new GameObject("Txt");
            ftTxt.transform.SetParent(ftBtnObj.transform, false);
            RectTransform fttRect = ftTxt.AddComponent<RectTransform>();
            fttRect.anchorMin = Vector2.zero;
            fttRect.anchorMax = Vector2.one;
            fttRect.offsetMin = new Vector2(10, 5);
            fttRect.offsetMax = new Vector2(-10, -5);
            TextMeshProUGUI fTmp = ftTxt.AddComponent<TextMeshProUGUI>();
            fTmp.text = "Fetch";
            fTmp.color = Color.white;
            fTmp.alignment = TextAlignmentOptions.Center;
            fTmp.fontSizeMax = 35;
            fTmp.fontSizeMin = 10;
            fTmp.enableAutoSizing = true;
            if (font != null) fTmp.font = font;
            ftTxt.AddComponent<Outline>().effectColor = Color.black;
            so.FindProperty("fetchButton").objectReferenceValue = fBtn;

            // Gold Label (Moved below the grid, matching design)
            GameObject goldLabel = new GameObject("GoldIcon");
            goldLabel.transform.SetParent(invPanel, false);
            RectTransform glRect = goldLabel.AddComponent<RectTransform>();
            glRect.anchorMin = new Vector2(0.1f, 0.1f);
            glRect.anchorMax = new Vector2(0.1f, 0.1f);
            glRect.anchoredPosition = Vector2.zero;
            glRect.sizeDelta = new Vector2(40, 40);
            goldLabel.AddComponent<Image>().sprite = coinSprite;

            GameObject goldTxt = new GameObject("GoldTxt");
            goldTxt.transform.SetParent(invPanel, false);
            RectTransform gtRect = goldTxt.AddComponent<RectTransform>();
            gtRect.anchorMin = new Vector2(0.14f, 0.1f);
            gtRect.anchorMax = new Vector2(0.14f, 0.1f);
            gtRect.anchoredPosition = Vector2.zero;
            gtRect.sizeDelta = new Vector2(150, 40);
            TextMeshProUGUI gTmp = goldTxt.AddComponent<TextMeshProUGUI>();
            gTmp.text = "1000";
            gTmp.fontSize = 35;
            gTmp.alignment = TextAlignmentOptions.Left;
            gTmp.color = Color.yellow;
            gTmp.fontStyle = FontStyles.Bold;
            if (font != null) gTmp.font = font;
            goldTxt.AddComponent<Outline>().effectColor = Color.black;
            so.FindProperty("goldText").objectReferenceValue = gTmp;

            so.ApplyModifiedProperties();
        }

    }
}
#endif
