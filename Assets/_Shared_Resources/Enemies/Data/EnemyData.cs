using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Gameplay/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 150;
    
    [Header("Movement")]
    public float moveSpeed = 5f;

}
