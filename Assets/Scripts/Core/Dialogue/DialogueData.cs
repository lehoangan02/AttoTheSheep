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
    public Sprite speakerAvatar;

    [Header("Dialogue Lines")]
    [TextArea(3, 6)]
    public string[] lines;
}
