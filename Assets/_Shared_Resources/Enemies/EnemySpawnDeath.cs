using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(EnemyEntity))]
public class EnemySpawnDeath : NetworkBehaviour
{
    private EnemyEntity entity;
    private EnemyBrain brain;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool isDying;
    private bool _despawned;

    void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        brain = GetComponent<EnemyBrain>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }

        if (!entity.IsAlive) return;

        SetAlpha(0f);

        if (brain != null)
            brain.IsFrozen = true;

        if (IsServer)
            entity.isInvulnerable.Value = true;

        if (IsServer && entity.Data != null && entity.Data.spawnVFXPrefab != null)
        {
            GameObject vfx = Instantiate(entity.Data.spawnVFXPrefab, transform.position, Quaternion.identity);
            NetworkObject vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null) vfxNetObj.Spawn();
        }

        float fadeDur = entity.Data != null ? entity.Data.spawnFadeDuration : 0.5f;
        StartCoroutine(SpawnFadeRoutine(fadeDur));
    }

    private IEnumerator SpawnFadeRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(t);
            yield return null;
        }
        SetAlpha(1f);

        if (brain != null)
            brain.IsFrozen = false;

        if (IsServer)
            entity.isInvulnerable.Value = false;
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderers == null) return;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            Color c = originalColors[i];
            c.a = alpha;
            spriteRenderers[i].color = c;
        }
    }

    public void PlayDeathSequence()
    {
        if (!IsServer || isDying) return;
        isDying = true;

        EnemyHitbox[] hitboxes = GetComponentsInChildren<EnemyHitbox>();
        foreach (var hitbox in hitboxes)
        {
            hitbox.Disable();
        }

        if (IsServer && entity.Data != null && entity.Data.deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(entity.Data.deathVFXPrefab, transform.position, Quaternion.identity);
            NetworkObject vfxNetObj = vfx.GetComponent<NetworkObject>();
            if (vfxNetObj != null) vfxNetObj.Spawn();
        }

        float fadeDur = entity.Data != null ? entity.Data.deathFadeDuration : 0.5f;
        PlayDeathFxClientRpc(fadeDur);

        StartCoroutine(DeathFadeThenDespawn(fadeDur));
    }

    [ClientRpc]
    private void PlayDeathFxClientRpc(float fadeDuration)
    {
        if (!IsServer)
        {
            StartCoroutine(DeathFadeRoutine(fadeDuration));
        }
    }

    private IEnumerator DeathFadeRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_despawned) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(1f - t);
            yield return null;
        }
        if (!_despawned) SetAlpha(0f);
    }

    private IEnumerator DeathFadeThenDespawn(float duration)
    {
        yield return DeathFadeRoutine(duration);
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        _despawned = true;
        StopAllCoroutines();
        base.OnNetworkDespawn();
    }
}
