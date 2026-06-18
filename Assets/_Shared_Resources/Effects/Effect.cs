using UnityEngine;

public enum EffectKind { Poison, Burn, Freeze, Stun, Knockback, Slow }

[CreateAssetMenu(menuName = "Gameplay/Effects/Effect")]
public class Effect : ScriptableObject
{
    public EffectKind kind;
    public float duration = 3f;
    public float tickRate = 1f;      // 0 = one-shot
    public int damagePerTick;
    public bool stopsMovement;
    public bool stopsAttack;
    public float knockbackForce;
    public float speedMultiplier = 1f; // <1 = slow

    public virtual void OnApply(StatusEffectController controller) {}
    public virtual void OnTick(StatusEffectController controller) {}
    public virtual void OnExpire(StatusEffectController controller) {}
}
