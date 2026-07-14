using UnityEngine;

[CreateAssetMenu(fileName = "TrollEnemyData", menuName = "Gameplay/Enemies/Troll Enemy Data")]
public class TrollEnemyData : EnemyData
{
    [Header("Smash")]
    public float smashRange = 2f;
    public float smashCooldown = 3f;
    public int smashDamage = 40;
    public StatusEffectData[] smashEffects;
    public float smashKnockbackForce = 8f;
    public float smashKnockbackDuration = 0.25f;
    public float smashWindupTime = 0.5f;
    public float smashMaxAttackTime = 2f;

    [Header("Charge")]
    public float chargeRange = 8f;
    public float chargeCooldown = 5f;
    public float chargeSpeed = 12f;
    public float chargeMaxDistance = 10f;
    public int chargeDamage = 35;
    public StatusEffectData[] chargeEffects;
    public float chargeKnockbackForce = 10f;
    public float chargeKnockbackDuration = 0.2f;
    public float chargeWindupTime = 0.5f;
    public float chargeMaxAttackTime = 3f;

    [Header("Tornado")]
    public float tornadoRange = 14f;
    public float tornadoCooldown = 9f;
    public float tornadoSpeed = 6f;
    public float tornadoDuration = 4f;
    public float tornadoTickInterval = 0.5f;
    public int tornadoDamagePerTick = 8;
    public float tornadoWindupTime = 0.5f;
    public float tornadoMaxAttackTime = 8f;

    [Header("Spikes")]
    public GameObject spikePrefab;
    public int spikeCount = 5;
    public float spikeSpacing = 1.2f;
    public float spikeSpawnInterval = 0.12f;

    [Header("Recovery")]
    public float recoveryInterval = 30f;
    public float recoveryDuration = 3f;
}
