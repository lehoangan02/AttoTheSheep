using UnityEngine;

[CreateAssetMenu(fileName = "SneezeSkillData", menuName = "Gameplay/Skills/Sneeze Skill Data")]
public class SneezeSkillData : SkillData
{
    [Header("Cài đặt Đạn Nước Mũi (Projectile)")]
    public GameObject projectilePrefab;
    public int projectileCount = 5;
    public float spreadAngle = 60f;
    public float projectileSpeed = 15f;
    public float projectileMaxDistance = 8f;

    [Header("Cài đặt Vùng Làm Chậm (Puddle)")]
    public GameObject puddlePrefab;
    public float puddleDuration = 5f;
    public StatusEffectData slowEffect;
    public LayerMask hitLayer;
}