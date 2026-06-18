using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Actions/Skill")]
public class SkillAction : EnemyAction
{
    public EnemySkill skill;

    public override void Execute(EnemyBrain brain)
    {
        if (skill == null) return;
        brain.Animator?.SetChasing(false);
        brain.Motor?.Stop();

        string key = skill.skillName;
        if (brain.GetCooldownTimer(key) < skill.cooldown) return;

        brain.SetCooldownTimer(key, 0f);
        brain.Animator?.PlaySkill(skill.animationTrigger);
        Vector3 targetPos = brain.target != null ? brain.target.transform.position : brain.transform.position;
        skill.Execute(brain, targetPos);
    }
}
