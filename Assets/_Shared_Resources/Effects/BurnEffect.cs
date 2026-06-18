using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Burn")]
public class BurnEffect : Effect
{
    public override void OnTick(StatusEffectController controller)
    {
        controller.Owner?.TakeDamage(damagePerTick);
    }
}
