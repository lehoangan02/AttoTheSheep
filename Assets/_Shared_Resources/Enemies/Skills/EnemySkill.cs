using UnityEngine;

public abstract class EnemySkill : ScriptableObject
{
    public string skillName;
    public float cooldown = 10f;
    public float castTime = 0.5f;
    public string animationTrigger;
    public Effect[] onHitEffects;     // effects applied on hit (Burn, Freeze, Stun, etc)
    public GameObject projectilePrefab; // null for AOE/self-buff skills
    public float aoeRadius;            // 0 = single target
    public float range = 5f;
    public int damage = 100;

    public abstract void Execute(EnemyBrain brain, Vector3 targetPosition);
}
