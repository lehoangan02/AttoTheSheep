using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Wait")]
public class WaitAction : EnemyAction
{
    public override void Execute(EnemyBrain brain)
    {
        brain.Animator?.SetChasing(false);
        brain.Motor?.Stop();
    }
}
