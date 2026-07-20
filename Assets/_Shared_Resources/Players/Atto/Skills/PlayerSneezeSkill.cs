using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PlayerSneezeSkill : BaseSkillComponent
{
    [Header("Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    private SneezeSkillData currentSneezeData;
    private PlayerController sneezeController;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is SneezeSkillData sneezeData && controller != null)
        {
            currentSneezeData = sneezeData;
            sneezeController = controller;
            StartCoroutine(SneezeRoutine(sneezeData, controller, caster));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        base.ClientPlayVisual(data);
    }

    private IEnumerator SneezeRoutine(SneezeSkillData data, PlayerController controller, NetworkEntity caster)
    {

        if (data.projectilePrefab == null)
        {

            yield break;
        }

        int damagePerProjectile = Mathf.Max(1, (int)data.damage / data.projectileCount);

        Vector2 baseDirection = GetFacingDirection(controller);

        bool flipX = baseDirection.x < 0;

        for (int i = 0; i < data.projectileCount; i++)
        {

            int index = flipX ? (data.projectileCount - 1 - i) : i;

            float halfSpread = data.spreadAngle * 0.5f;
            float angleStep = data.projectileCount > 1 ? data.spreadAngle / (data.projectileCount - 1) : 0f;
            float currentAngle = -halfSpread + (angleStep * index);

            Vector2 projectileDir = RotateVector(baseDirection, currentAngle);

            Vector3 spawnPos = controller.transform.position;
            if (spawnPoint != null) spawnPos = spawnPoint.position;

            // Instantiate prefab
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

            NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            SneezeProjectile projectile = projectileObj.GetComponent<SneezeProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(projectileDir, data, damagePerProjectile, flipX, caster);
            }
        }

        yield return null;
    }

    /// <summary>

    /// </summary>
    private Vector2 GetFacingDirection(PlayerController controller)
    {

        Rigidbody2D rb = controller.GetComponent<Rigidbody2D>();
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            return rb.linearVelocity.normalized;
        }

        SpriteRenderer sprite = controller.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null && sprite.flipX)
        {
            return Vector2.left;
        }
        return Vector2.right;
    }

    /// <summary>

    /// </summary>
    private Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentSneezeData == null) return;

        Gizmos.color = Color.cyan;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 baseDir = Vector2.right;

        float halfSpread = currentSneezeData.spreadAngle * 0.5f;
        float angleStep = currentSneezeData.projectileCount > 1
            ? currentSneezeData.spreadAngle / (currentSneezeData.projectileCount - 1)
            : 0f;

        for (int i = 0; i < currentSneezeData.projectileCount; i++)
        {
            float currentAngle = -halfSpread + (angleStep * i);
            Vector2 dir = RotateVector(baseDir, currentAngle);
            Gizmos.DrawRay(pos, dir * currentSneezeData.projectileMaxDistance);
        }
    }
}