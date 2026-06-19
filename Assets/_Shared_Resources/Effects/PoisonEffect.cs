using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Poison")]
public class PoisonEffect : Effect
{
    public override void OnTick(StatusEffectController controller)
    {
        controller.Owner?.TakeDamage(damagePerTick);
    }
}
