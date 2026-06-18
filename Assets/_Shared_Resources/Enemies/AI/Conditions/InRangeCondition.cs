using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Conditions/In Range")]
public class InRangeCondition : EnemyCondition
{
    public float range = 1.2f;
    public override bool Evaluate(EnemyBrain brain)
    {
        if (brain.target == null) return false;
        return Vector2.Distance(brain.transform.position, brain.target.transform.position) <= range;
    }
}
