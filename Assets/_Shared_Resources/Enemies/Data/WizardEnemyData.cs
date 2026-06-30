using UnityEngine;

[CreateAssetMenu(fileName = "WizardEnemyData", menuName = "Gameplay/Enemies/Wizard Enemy Data")]
public class WizardEnemyData : EnemyData
{
    [Header("Throw")]
    public float throwRange = 8f;
    public float throwCooldown = 1.5f;
    public int throwDamage = 40;
    public float ballSpeed = 7f;
    public GameObject explosionBallPrefab;

    [Header("Transform")]
    public float transformRange = 3f;
    public float transformCooldown = 8f;
    public GameObject transformSpellVFXPrefab;
    public GameObject pigPrefab;
}
