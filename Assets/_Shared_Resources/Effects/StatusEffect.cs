using UnityEngine;

/// <summary>
/// Runtime instance of a status effect active on an entity.
/// Held in <see cref="StatusEffectController"/>'s active list.
/// Plain C# class — no Unity lifecycle overhead.
/// </summary>
public class StatusEffect
{
    /// <summary>The data asset defining this effect.</summary>
    public StatusEffectSO Data { get; }

    /// <summary>The entity that applied this effect (for damage attribution, knockback direction).</summary>
    public NetworkEntity Source { get; }

    /// <summary>Remaining duration in seconds. <see cref="float.MaxValue"/> = indefinite.</summary>
    public float RemainingDuration { get; set; }

    /// <summary>Accumulated time since the last tick.</summary>
    public float TickTimer { get; set; }

    /// <summary>Resolved damage per tick (override or SO default). Settable for RefreshDuration stacking.</summary>
    public int DamagePerTick { get; set; }

    /// <summary>True when the effect has no expiry (area effects, manual removal).</summary>
    public bool IsIndefinite => RemainingDuration >= float.MaxValue;

    protected StatusEffect(StatusEffectSO data, NetworkEntity source, float duration, int damagePerTickOverride)
    {
        Data = data;
        Source = source;
        RemainingDuration = duration > 0f ? duration : (data.duration > 0f ? data.duration : float.MaxValue);
        DamagePerTick = damagePerTickOverride > 0 ? damagePerTickOverride : data.damagePerTick;
    }

    /// <summary>Called once when the effect is applied to the target.</summary>
    public virtual void OnApply(IStatusTarget target, StatusEffectController controller) { }

    /// <summary>Called at each tick interval while the effect is active. Base implementation deals DoT damage.</summary>
    public virtual void OnTick(IStatusTarget target, StatusEffectController controller)
    {
        if (DamagePerTick > 0)
            target.TakeDamage(DamagePerTick, Source);
    }

    /// <summary>Called once when the effect expires or is removed.</summary>
    public virtual void OnExpire(IStatusTarget target, StatusEffectController controller) { }

    // ------------------------------------------------------------------ //
    //  Factory
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Create the correct runtime instance for the given data asset.
    /// Most effects use the base <see cref="StatusEffect"/> (data-driven).
    /// Knockback needs a custom subclass for directional impulse logic.
    /// To add a new custom effect: add one case here + one subclass.
    /// </summary>
    public static StatusEffect Create(StatusEffectSO data, NetworkEntity source,
        float duration, int damagePerTickOverride = 0)
    {
        if (data.kind == EffectKind.Knockback)
            return new KnockbackStatusEffect(data, source, duration, damagePerTickOverride);
        return new StatusEffect(data, source, duration, damagePerTickOverride);
    }
}

/// <summary>
/// Knockback effect — applies a directional impulse on apply, then immediately
/// expires. Kept in the status effect system for a unified API so callers
/// don't need a separate knockback code path.
/// </summary>
public class KnockbackStatusEffect : StatusEffect
{
    public KnockbackStatusEffect(StatusEffectSO data, NetworkEntity source, float duration, int damagePerTickOverride)
        : base(data, source, duration, damagePerTickOverride) { }

    public override void OnApply(IStatusTarget target, StatusEffectController controller)
    {
        if (Source == null) return;
        Vector2 dir = ((Vector2)controller.transform.position - (Vector2)Source.transform.position).normalized;
        target.ApplyKnockback(dir * Data.knockbackForce, Data.knockbackDuration);
    }
}
