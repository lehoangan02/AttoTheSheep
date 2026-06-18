using UnityEngine;

public enum SpeedTier { Basic, Fast, Slow }

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public EnemyKind enemyKind;
    public string displayName;

    [Header("Base Stats")]
    public int maxHealth = 150;
    public SpeedTier speedTier = SpeedTier.Basic;
    
    [Header("Movement")]
    public float moveSpeed = 5f;        // overridable per enemy; SpeedTier sets default

    [Header("Attacks")]
    public AttackData[] attacks;        // attack variants. Index 0 = default.
    public EffectData[] onHitEffects;   // passive effects on all basic attacks (PoisonSnake, PoisonSpider)

    [Header("Skills")]
    public EnemySkill[] skills;         // special abilities (Wizard Fireball/Iceball, Demon Smash/Tornado/RollDash)

    [Header("References")]
    public EnemyBehavior behavior;      // the behavior tree asset for this enemy kind
    public GameObject projectilePrefab; // null for melee enemies

    // Derived helpers
    public float MoveSpeed => moveSpeed > 0 ? moveSpeed : GetDefaultSpeed();
    public int AttackDamage => attacks != null && attacks.Length > 0 ? attacks[0].damage : 20;
    public float AttackRange => attacks != null && attacks.Length > 0 ? attacks[0].range : 1.2f;
    public float AttackCooldown => attacks != null && attacks.Length > 0 ? attacks[0].cooldown : 1.25f;

    float GetDefaultSpeed()
    {
        return speedTier switch
        {
            SpeedTier.Fast => 8f,
            SpeedTier.Slow => 3f,
            _ => 5f
        };
    }
}
