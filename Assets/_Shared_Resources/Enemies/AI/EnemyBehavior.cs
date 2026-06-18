using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/AI/Enemy Behavior")]
public class EnemyBehavior : ScriptableObject
{
    public BehaviorNode[] nodes; // evaluated top-to-bottom. First matching node executes.
}
