using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Stun")]
public class StunEffect : Effect
{
    public override void OnApply(StatusEffectController controller)
    {
        if (controller.OwnerBrain != null)
            controller.OwnerBrain.IsStunned = true;
    }

    public override void OnExpire(StatusEffectController controller)
    {
        if (controller.OwnerBrain != null)
            controller.OwnerBrain.IsStunned = false;
    }
}
