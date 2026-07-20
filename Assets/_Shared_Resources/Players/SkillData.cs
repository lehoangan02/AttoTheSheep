using UnityEngine;

[System.Serializable]
public class SkillAudio
{
    public AudioClip clip;

    [Tooltip("Thời gian tối đa để phát âm thanh (Giây). Nếu bằng 0, sẽ phát hết toàn bộ file.")]
    public float duration = 0f;

    public bool HasAudio()
    {
        return clip != null;
    }
}

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Gameplay/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Information")]
    public int skillId;
    public string skillName;
    [TextArea(2, 5)] public string description;
    public Sprite skillIcon;

    [Header("Base Stats (Edit in Unity)")]
    public float cooldown = 5f;
    public float damage = 10f;
    public float manaCost = 0f;
    public int lambsRequired = 0;

    [Header("Visual Effects")]
    public GameObject vfxPrefab;

    [Header("Audio Effects (Tùy chọn từng giai đoạn)")]
    [Tooltip("Phát khi vừa bấm nút tung chiêu (Tiếng lấy đà, hít hơi, xé gió)")]
    public SkillAudio castSFX;

    [Tooltip("Phát khi chiêu đang hoạt động (Tiếng lướt, cuộn tròn, tiếng xì hơi kéo dài)")]
    public SkillAudio activeSFX;

    [Tooltip("Phát khi va chạm vào kẻ địch (Tiếng Bốp, Chát, Nổ)")]
    public SkillAudio hitSFX;
}