using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;

[RequireComponent(typeof(EnemyEntity))]
public class EnemySpawnDeath : NetworkBehaviour
{
    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Header("Spawn / Death VFX")]
    [SerializeField] private VisualEffect spawnVFX;
    [SerializeField] private VisualEffect deathVFX;
    [SerializeField] private float spawnFadeDuration = 0.5f;
    [SerializeField] private float deathFadeDuration = 0.5f;

    private EnemyEntity entity;
    private EnemyBrain brain;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private MaterialPropertyBlock flashPropertyBlock;
    private bool isDying;
    private bool _despawned;

    void Awake()
    {
        entity = GetComponent<EnemyEntity>();
        brain = GetComponent<EnemyBrain>();
        flashPropertyBlock = new MaterialPropertyBlock();
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

        entity.currentHealth.OnValueChanged += OnEnemyHealthChanged;

        if (!entity.IsAlive) return;

        SetAlpha(0f);

        if (brain != null)
            brain.IsFrozen = true;

        if (IsServer)
            entity.isInvulnerable.Value = true;

        if (IsServer && spawnVFX != null)
            spawnVFX.Play();

        StartCoroutine(SpawnFadeRoutine(spawnFadeDuration));
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

    private void OnEnemyHealthChanged(int previousValue, int newValue)
    {
        if (newValue < previousValue)
        {
            StopCoroutine(nameof(DamageFlashRoutine));
            StartCoroutine(DamageFlashRoutine());
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderers == null) yield break;

        flashPropertyBlock.SetColor("_Flash", damageFlashColor);
        flashPropertyBlock.SetFloat("_Amount", 1f);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].SetPropertyBlock(flashPropertyBlock);
        }

        yield return new WaitForSeconds(damageFlashDuration);

        flashPropertyBlock.SetFloat("_Amount", 0f);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].SetPropertyBlock(flashPropertyBlock);
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

        if (IsServer && deathVFX != null)
            deathVFX.Play();

        PlayDeathFxClientRpc(deathFadeDuration);

        StartCoroutine(DeathFadeThenDespawn(deathFadeDuration));
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
        entity.currentHealth.OnValueChanged -= OnEnemyHealthChanged;
        _despawned = true;
        StopAllCoroutines();
        base.OnNetworkDespawn();
    }
}
