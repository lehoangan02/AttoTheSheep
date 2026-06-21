using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class MainMenuUIBuilder : MonoBehaviour
{
    [SerializeField] private MainMenuUIConfig config;
    [SerializeField] private MainMenuController controller;

    private const string RootName = "MainMenuRoot";

    [System.Obsolete("MainMenuUIBuilder is disabled. Use Tools > Setup KhoaMenu UI to bake the UI.")]
    private void Awake()
    {
        // This component has been superseded by the KhoaMenuSetup Editor tool.
        // The UI is now baked directly into the scene at design time.
        // Destroy this component immediately so it never generates duplicate UI.
        Destroy(this);
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.referenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void BuildMenu(Transform canvasTransform)
    {
        var root = CreateRect(RootName, canvasTransform);
        StretchFull(root);

        var background = CreateImage("Background", root, config.backgroundSprite, config.backgroundTint);
        StretchFull(background.rectTransform);
        if (config.backgroundSprite != null)
            background.type = Image.Type.Tiled;

        var title = CreateImage("TitleRibbon", root, config.titleRibbonSprite, Color.white);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(520f, 110f));

        var titleText = CreateText("TitleText", title.transform, "ATTO THE SHEEP", config.titleFontSize, config.titleColor, TextAlignmentOptions.Center);
        StretchFull(titleText.rectTransform);

        var panel = CreateImage("MenuPanel", root, config.panelSprite, new Color(1f, 1f, 1f, 0.92f));
        SetAnchored(panel.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(480f, 420f));
        if (config.panelSprite != null)
            panel.type = Image.Type.Sliced;

        var buttonContainer = CreateRect("ButtonContainer", panel.transform);
        SetAnchored(buttonContainer, new Vector2(0.5f, 0.5f), new Vector2(config.buttonWidth + 40f, 300f));

        var layout = buttonContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = config.buttonSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = buttonContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var newGameButton = CreateMenuButton(buttonContainer, "NewGameButton", "New Game", 0f);
        var continueButton = CreateMenuButton(buttonContainer, "ContinueButton", "Continue", 0.12f);
        var multiplayerButton = CreateMenuButton(buttonContainer, "MultiplayerButton", "Multiplayer", 0.24f);

        var fadeOverlay = CreateImage("FadeOverlay", root, null, Color.black);
        StretchFull(fadeOverlay.rectTransform);
        fadeOverlay.raycastTarget = false;
        var fadeGroup = fadeOverlay.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        if (controller == null)
            controller = GetComponent<MainMenuController>();

        if (controller != null)
            // UIBuilder is obsolete - setup is now done by KhoaMenuSetup editor tool.
            // Awake() destroys this component before BuildMenu() runs; this line is dead code
            // but must compile. Pass typed nulls for the new parameters.
            controller.Setup(newGameButton, continueButton, multiplayerButton, (Button)null, (GameObject)null, fadeGroup);
    }

    private Button CreateMenuButton(Transform parent, string name, string label, float entranceDelay)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonAnimator));
        buttonGo.transform.SetParent(parent, false);

        var rect = buttonGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(config.buttonWidth, config.buttonHeight);

        var image = buttonGo.GetComponent<Image>();
        image.sprite = config.buttonSprite;
        image.type = config.buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;

        var button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        button.colors = colors;

        buttonGo.GetComponent<MenuButtonAnimator>().SetEntranceDelay(entranceDelay);

        var text = CreateText("Text", buttonGo.transform, label, config.buttonFontSize, config.buttonTextColor, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        text.raycastTarget = false;

        return button;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = sprite != null;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.font = config.font;
        return text;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

}
