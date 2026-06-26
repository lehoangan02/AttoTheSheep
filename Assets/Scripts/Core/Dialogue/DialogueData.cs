using UnityEngine;

/// <summary>
/// Holds all data for a single NPC dialogue sequence.
/// Create via: Right-click > Create > Dialogue > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Speaker Info")]
    public string speakerName = "Unknown";
    public string speakerNameVietnamese = "";
    public Sprite speakerAvatar;

    [Header("Dialogue Lines (Mặc định - English)")]
    [TextArea(3, 6)]
    public string[] lines;

    [Header("Dialogue Lines (Vietnamese)")]
    [TextArea(3, 6)]
    public string[] vietnameseLines;
}
