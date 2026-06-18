using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Guard")]
public class GuardAction : EnemyAction
{
    public override void Execute(EnemyBrain brain)
    {
        brain.Animator?.SetChasing(false);
        brain.Motor?.Stop();
        brain.Animator?.PlayGuard();
    }
}
