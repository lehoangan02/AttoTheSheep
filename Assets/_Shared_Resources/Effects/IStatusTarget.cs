using UnityEngine;

/// <summary>
/// Contract for any entity that can receive status effects.
/// Implemented by <see cref="NetworkEntity"/> (base for Player, Lamb, Enemy).
/// Decouples status effects from concrete entity types (EnemyBrain, EnemyMotor, etc.).
/// </summary>
public interface IStatusTarget
{
    /// <summary>Transform where effect VFX should be parented. Falls back to the entity root.</summary>
    Transform VfxAnchor { get; }

    /// <summary>Apply damage to the entity. Server-only.</summary>
    void TakeDamage(int amount, NetworkEntity source);

    /// <summary>Apply a knockback impulse to the entity. Server-only.</summary>
    void ApplyKnockback(Vector2 force, float duration);
}
