using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Gameplay/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Thông Tin Cơ Bản")]
    public int skillId;
    public string skillName;
    [TextArea(2, 5)] public string description;

    [Header("Bộ Chỉ Số Base Stats (Chỉnh trong Unity)")]
    public float cooldown = 5f;
    public float damage = 10f;
    public float manaCost = 0f;
    public int lambsRequired = 0; // Mốc số cừu để unlock chiêu này

    [Header("Hiệu Ứng hình ảnh (Nếu có)")]
    public GameObject vfxPrefab;
}