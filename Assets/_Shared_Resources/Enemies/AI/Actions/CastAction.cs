using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Cast")]
public class CastAction : EnemyAction
{
    public EnemySkill skill;  // reference to skill asset

    public override void Execute(EnemyBrain brain)
    {
        if (brain.target == null || skill == null) return;
        brain.Animator?.SetChasing(false);
        brain.Motor?.Stop();

        string key = skill.skillName;
        if (brain.GetCooldownTimer(key) < skill.cooldown) return;

        brain.SetCooldownTimer(key, 0f);
        brain.Animator?.PlayCast(skill.animationTrigger);
        skill.Execute(brain, brain.target.transform.position);
    }
}
