using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Slow")]
public class SlowEffect : Effect
{
    public override void OnApply(StatusEffectController controller)
    {
        controller.OwnerMotor?.SetSpeedMultiplier(speedMultiplier);
    }

    public override void OnExpire(StatusEffectController controller)
    {
        controller.OwnerMotor?.SetSpeedMultiplier(1f);
    }
}
