using UnityEngine;

[System.Serializable]
public class BehaviorNode
{
    public EnemyCondition condition;
    public EnemyAction action;
    // Simple: evaluate condition → if true, execute action.
    // If condition is null, always true.
}
