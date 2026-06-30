using UnityEngine;

/// <summary>Identifies an effect type for queries (HasEffect, RemoveEffect, VFX mapping).</summary>
public enum EffectKind { Poison, Burn, Freeze, Stun, Knockback, Slow }

/// <summary>How to handle applying an effect when one of the same kind is already active.</summary>
public enum EffectStacking
{
    /// <summary>Expire the old effect and add a fresh one.</summary>
    Replace,
    /// <summary>Reset the remaining duration of the existing effect (no new instance).</summary>
    RefreshDuration
}

/// <summary>
/// Pure data definition for a status effect. No behavior — all logic is
/// data-driven (DoT damage, movement/attack locks, speed multiplier) or
/// handled by <see cref="StatusEffect"/> runtime instances.
/// Create assets via Create &gt; Gameplay &gt; Effects &gt; Status Effect.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Effects/Status Effect")]
public class StatusEffectSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Effect category — used for stacking, queries, and VFX mapping.")]
    public EffectKind kind;

    [Header("Timing")]
    [Tooltip("Duration in seconds. 0 = indefinite (removed manually, e.g. area effects).")]
    public float duration = 3f;

    [Tooltip("Seconds between OnTick calls. 0 = no ticking.")]
    public float tickRate = 1f;

    [Header("Damage over Time")]
    [Tooltip("Damage dealt each tick (Poison, Burn). 0 = no damage.")]
    public int damagePerTick;

    [Header("Crowd Control")]
    [Tooltip("Entity cannot move while this effect is active (Freeze, Stun).")]
    public bool stopsMovement;

    [Tooltip("Entity cannot attack while this effect is active.")]
    public bool stopsAttack;

    [Header("Movement Modifier")]
    [Tooltip("Speed multiplier while active. <1 = slow, >1 = haste, 1 = no change.")]
    public float speedMultiplier = 1f;

    [Header("Knockback")]
    [Tooltip("Knockback impulse magnitude. Only used by Knockback effects.")]
    public float knockbackForce;

    [Tooltip("How long the knockback impulse lasts.")]
    public float knockbackDuration = 0.2f;

    [Header("Stacking")]
    [Tooltip("Replace = expire old + add new. RefreshDuration = reset timer on existing.")]
    public EffectStacking stacking = EffectStacking.Replace;
}
