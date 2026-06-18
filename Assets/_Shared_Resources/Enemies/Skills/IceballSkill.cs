using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(menuName = "Gameplay/Skills/Iceball")]
public class IceballSkill : EnemySkill
{
    public override void Execute(EnemyBrain brain, Vector3 targetPosition)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = brain.transform.position;
        Vector2 direction = (targetPosition - spawnPos).normalized;

        GameObject projObj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.Initialize(direction, 7f, damage, onHitEffects, brain.Entity);
        }

        NetworkObject netObj = projObj.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn(true);
    }
}
