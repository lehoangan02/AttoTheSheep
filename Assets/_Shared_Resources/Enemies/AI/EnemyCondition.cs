using UnityEngine;

public abstract class EnemyCondition : ScriptableObject
{
    public abstract bool Evaluate(EnemyBrain brain);
}
