using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "MainMenuUIConfig", menuName = "AttoTheSheep/Main Menu UI Config")]
public class MainMenuUIConfig : ScriptableObject
{
    [Header("Sprites (Tiny Swords)")]
    public Sprite backgroundSprite;
    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite titleRibbonSprite;

    [Header("Typography")]
    public TMP_FontAsset font;
    public int titleFontSize = 48;
    public int buttonFontSize = 28;

    [Header("Colors")]
    public Color backgroundTint = new Color(0.15f, 0.35f, 0.55f, 1f);
    public Color titleColor = new Color(1f, 0.95f, 0.75f, 1f);
    public Color buttonTextColor = Color.white;

    [Header("Layout")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    public float buttonWidth = 360f;
    public float buttonHeight = 72f;
    public float buttonSpacing = 24f;
}
