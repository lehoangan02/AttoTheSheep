using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Conditions/Random Chance")]
public class RandomChanceCondition : EnemyCondition
{
    [Range(0f, 1f)] public float chance = 0.5f;
    public override bool Evaluate(EnemyBrain brain)
    {
        return Random.value <= chance;
    }
}
