using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Conditions/Has Target")]
public class HasTargetCondition : EnemyCondition
{
    public override bool Evaluate(EnemyBrain brain)
    {
        return brain.target != null;
    }
}
