using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerRollingSkill : BaseSkillComponent
{
    [Header("Visual References")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject dustVisual;
    [SerializeField] private Transform spinMesh;
    [SerializeField] private ParticleSystem rollingDust;

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private PlayerController rollController;

    private class SwallowedEnemy
    {
        public GameObject Obj;
        public EnemyBrain Brain;
        public EnemyMotor Motor;
        public NetworkEntity Entity;
        public SpriteRenderer[] Renderers;
        public Collider2D[] Colliders;
        public Vector3 OriginalScale;
    }

    private List<SwallowedEnemy> stomach = new List<SwallowedEnemy>();

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is RollingSkillData rollData && controller != null)
        {
            rollController = controller;
            StartCoroutine(RollingRoutine(rollData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        base.ClientPlayVisual(data);
        if (data is RollingSkillData rollData)
        {
            StartCoroutine(ClientRollingVisualRoutine(rollData));
        }
    }

    private IEnumerator ClientRollingVisualRoutine(RollingSkillData data)
    {
        if (normalVisual != null) normalVisual.SetActive(false);
        if (dustVisual != null) dustVisual.SetActive(true);
        if (rollingDust != null) rollingDust.Play();

        float elapsed = 0f;
        float currentMultiplier = 1f;

        while (elapsed < data.duration)
        {
            float deltaTime = Time.deltaTime;
            if (currentMultiplier < data.maxSpeedMultiplier)
            {
                currentMultiplier += data.acceleration * deltaTime;
                currentMultiplier = Mathf.Min(currentMultiplier, data.maxSpeedMultiplier);
            }

            if (spinMesh != null)
            {
                float currentRotationSpeed = data.baseRotationSpeed * currentMultiplier;
                spinMesh.Rotate(0, 0, -currentRotationSpeed * deltaTime);
            }

            elapsed += deltaTime;
            yield return null;
        }

        if (rollingDust != null) rollingDust.Stop();
        if (dustVisual != null) dustVisual.SetActive(false);
        if (normalVisual != null) normalVisual.SetActive(true);
        if (spinMesh != null) spinMesh.localRotation = Quaternion.identity;

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }

    private IEnumerator RollingRoutine(RollingSkillData data, PlayerController controller)
    {

        PlayerMovement pMovement = controller.GetComponentInChildren<PlayerMovement>();
        if (pMovement == null) pMovement = controller.GetComponentInParent<PlayerMovement>();

        float elapsed = 0f;
        float currentMultiplier = 1f;

        while (elapsed < data.duration)
        {
            float deltaTime = Time.fixedDeltaTime;

            if (currentMultiplier < data.maxSpeedMultiplier)
            {
                currentMultiplier += data.acceleration * deltaTime;
                currentMultiplier = Mathf.Min(currentMultiplier, data.maxSpeedMultiplier);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(controller.transform.position, data.pullRadius, data.enemyLayer);
            foreach (var hit in hits)
            {
                float distance = Vector2.Distance(controller.transform.position, hit.transform.position);

                if (distance <= data.hitRadius)
                {
                    SwallowEnemy(hit.gameObject);
                }
                else
                {

                    hit.transform.position = Vector3.MoveTowards(
                        hit.transform.position,
                        controller.transform.position,
                        data.pullSpeed * deltaTime
                    );
                }
            }

            for (int i = stomach.Count - 1; i >= 0; i--)
            {
                SwallowedEnemy swallowed = stomach[i];
                if (swallowed.Obj == null)
                {
                    stomach.RemoveAt(i);
                    continue;
                }
                swallowed.Obj.transform.position = controller.transform.position;
            }

            elapsed += deltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Get damage multiplier from PlayerSkills
        float damageMultiplier = 1f;
        if (rollController != null)
        {
            PlayerSkills skills = rollController.GetComponentInChildren<PlayerSkills>();
            if (skills == null) skills = rollController.GetComponentInParent<PlayerSkills>();
            if (skills != null) damageMultiplier = skills.damageMultiplier.Value;
        }
        int burstDamage = Mathf.RoundToInt(data.damage * damageMultiplier);

        for (int i = stomach.Count - 1; i >= 0; i--)
        {
            SwallowedEnemy swallowed = stomach[i];

            if (swallowed.Obj == null)
            {
                stomach.RemoveAt(i);
                continue;
            }

            if (swallowed.Entity != null)
            {

                swallowed.Entity.TakeDamage(burstDamage);

                if (!swallowed.Entity.IsAlive)
                {

                    stomach.RemoveAt(i);
                }
            }
        }

        SpitOutEnemies(data);
    }

    private void SwallowEnemy(GameObject enemyObj)
    {
        if (stomach.Exists(e => e.Obj == enemyObj || e.Obj == enemyObj.transform.parent?.gameObject)) return;

        NetworkObject netObj = enemyObj.GetComponentInParent<NetworkObject>();
        if (netObj == null) netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj == null) return;

        GameObject rootObj = netObj.gameObject;
        SwallowedEnemy swallowed = new SwallowedEnemy { Obj = rootObj };

        swallowed.Brain = rootObj.GetComponent<EnemyBrain>();
        swallowed.Motor = rootObj.GetComponent<EnemyMotor>();
        swallowed.Entity = rootObj.GetComponent<NetworkEntity>();
        swallowed.Renderers = rootObj.GetComponentsInChildren<SpriteRenderer>();
        swallowed.Colliders = rootObj.GetComponentsInChildren<Collider2D>();
        swallowed.OriginalScale = rootObj.transform.localScale;

        if (swallowed.Brain != null) swallowed.Brain.IsFrozen = true;
        if (swallowed.Motor != null) swallowed.Motor.IsFrozen = true;

        foreach (var col in swallowed.Colliders) if (col != null) col.enabled = false;
        Rigidbody2D rb = rootObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        stomach.Add(swallowed);

        StartCoroutine(VisualSuckIn(swallowed));
    }

    private IEnumerator VisualSuckIn(SwallowedEnemy enemy)
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startPos = enemy.Obj.transform.position;

        while (elapsed < duration && enemy.Obj != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            enemy.Obj.transform.position = Vector3.Lerp(startPos, rollController.transform.position, t);

            enemy.Obj.transform.localScale = Vector3.Lerp(enemy.OriginalScale, Vector3.zero, t);

            yield return null;
        }

        if (enemy.Obj != null)
        {
            foreach (var sr in enemy.Renderers) if (sr != null) sr.enabled = false;

            if (enemy.Entity != null && enemy.Entity.NetworkObject != null)
            {
                SetEnemyVisibilityClientRpc(enemy.Entity.NetworkObject.NetworkObjectId, false);
            }

            enemy.Obj.transform.localScale = enemy.OriginalScale;
        }
    }

    private void SpitOutEnemies(RollingSkillData data)
    {
        if (stomach.Count == 0) return;

        List<SwallowedEnemy> enemiesToSpit = new List<SwallowedEnemy>(stomach);
        stomach.Clear();

        StartCoroutine(VisualPushOut(enemiesToSpit, data));
    }

    private IEnumerator VisualPushOut(List<SwallowedEnemy> enemiesToSpit, RollingSkillData data)
    {
        int enemyCount = enemiesToSpit.Count;
        float angleStep = 360f / enemyCount;
        float startAngle = Random.Range(0f, 360f);
        float baseRadius = data.pullRadius;

        Vector3[] targetPositions = new Vector3[enemyCount];
        Vector3 centerPos = rollController.transform.position;

        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = enemiesToSpit[i];
            if (enemy.Obj == null) continue;

            float currentAngle = startAngle + (i * angleStep);
            Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            float randomRadius = baseRadius + Random.Range(-0.3f, 0.5f);
            targetPositions[i] = (Vector2)centerPos + (direction * randomRadius);

            enemy.Obj.transform.position = centerPos;
            if (enemy.Renderers != null)
            {
                foreach (var sr in enemy.Renderers) if (sr != null) sr.enabled = true;
            }
            if (enemy.Entity != null && enemy.Entity.NetworkObject != null)
            {
                SetEnemyVisibilityClientRpc(enemy.Entity.NetworkObject.NetworkObjectId, true);
            }
        }

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = enemiesToSpit[i];
                if (enemy.Obj != null)
                {
                    enemy.Obj.transform.position = Vector3.Lerp(centerPos, targetPositions[i], easeOutT);
                }
            }
            yield return null;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = enemiesToSpit[i];
            if (enemy.Obj == null) continue;

            enemy.Obj.transform.position = targetPositions[i];

            Rigidbody2D rb = enemy.Obj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector2.zero;
            }

            if (enemy.Brain != null) enemy.Brain.IsFrozen = false;
            if (enemy.Motor != null) enemy.Motor.IsFrozen = false;
            if (enemy.Colliders != null)
                foreach (var col in enemy.Colliders) if (col != null) col.enabled = true;
        }
    }

    [ClientRpc]
    private void SetEnemyVisibilityClientRpc(ulong enemyNetId, bool isVisible)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyNetId, out var netObj))
        {
            var renderers = netObj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = isVisible;
            }
        }
    }
}