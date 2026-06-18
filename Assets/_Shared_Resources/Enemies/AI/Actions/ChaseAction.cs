using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Chase")]
public class ChaseAction : EnemyAction
{
    public override void Execute(EnemyBrain brain)
    {
        if (brain.target == null) return;
        brain.Animator?.SetChasing(true);
        float speed = brain.Entity?.MoveSpeed ?? 5f;
        speed *= brain.EffectController?.GetSpeedMultiplier() ?? 1f;
        brain.Motor?.MoveToward(brain.target.transform.position, speed);
    }
}
