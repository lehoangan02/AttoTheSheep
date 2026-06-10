using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Gameplay/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Information")]
    public int skillId;
    public string skillName;
    [TextArea(2, 5)] public string description;

    [Header("Base Stats (Edit in Unity)")]
    public float cooldown = 5f;
    public float damage = 10f;
    public float manaCost = 0f;
    public int lambsRequired = 0; // Required number of lambs to unlock this skill

    [Header("Visual Effects (If any)")]
    public GameObject vfxPrefab;
}