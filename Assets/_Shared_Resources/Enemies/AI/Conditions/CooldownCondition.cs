using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Conditions/Cooldown Ready")]
public class CooldownCondition : EnemyCondition
{
    public string cooldownKey;
    public float cooldown;
    public override bool Evaluate(EnemyBrain brain)
    {
        return brain.GetCooldownTimer(cooldownKey) >= cooldown;
    }
}
