using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Knockback")]
public class KnockbackEffect : Effect
{
    public override void OnApply(StatusEffectController controller)
    {
        if (controller.OwnerMotor == null) return;

        NetworkEntity source = controller.GetSource(EffectKind.Knockback);
        if (source == null) return;

        Vector2 sourceDirection = (controller.transform.position - source.transform.position).normalized;
        controller.OwnerMotor.ApplyKnockback(sourceDirection * knockbackForce, 0.2f);
    }
}
