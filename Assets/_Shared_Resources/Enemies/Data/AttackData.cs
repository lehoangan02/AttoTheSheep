using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Enemies/Attack Data")]
public class AttackData : ScriptableObject
{
    public string animationTrigger = "Attack";  // "Attack", "AttackFast", "AttackStrong", "AttackLeft", "AttackRight"
    public int damage;                          // overrides entity base damage
    public float range = 1.2f;                  // attack range
    public float cooldown = 1.25f;              // seconds between uses
    public EffectData[] onHitEffects;           // effects applied on hit (poison, burn, etc)
    public bool isDefault;                      // is this the primary attack?
    [Range(0f, 1f)] public float weight = 1f;  // for random selection (Goblin 70% fast / 30% strong)
    public bool causesKnockback;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Area of Effect")]
    public bool isAreaAttack;                   // true = hits all targets within areaRadius
    public float areaRadius = 1.5f;             // AoE radius (0 = use range)
    public LayerMask targetLayers = ~0;         // which layers to hit (default = all)
    public int maxTargets = -1;                 // -1 = unlimited, otherwise cap hits
    public AnimationCurve damageFalloff = AnimationCurve.Constant(0, 1, 1);
}
