using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Freeze")]
public class FreezeEffect : Effect
{
    public override void OnApply(StatusEffectController controller)
    {
        if (controller.OwnerBrain != null)
            controller.OwnerBrain.IsFrozen = true;
    }

    public override void OnExpire(StatusEffectController controller)
    {
        if (controller.OwnerBrain != null)
            controller.OwnerBrain.IsFrozen = false;
    }
}
