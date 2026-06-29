using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.VFX;

[RequireComponent(typeof(Collider2D))]
public class DestructibleBuilding : NetworkBehaviour
{
    [Header("Destruction VFX")]
    [SerializeField] private VisualEffect destructionVFX;
    [SerializeField] private float destructionFadeDuration = 0.5f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool isDestroying;
    private bool _despawned;
    private Collider2D _collider;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    public void PlayDestructionSequence()
    {
        if (!IsServer || isDestroying) return;
        isDestroying = true;

        if (_collider != null)
            _collider.enabled = false;

        if (destructionVFX != null)
            destructionVFX.Play();

        PlayDestructionFxClientRpc(destructionFadeDuration);
        StartCoroutine(DestructionFadeThenDespawn(destructionFadeDuration));
    }

    [ClientRpc]
    private void PlayDestructionFxClientRpc(float fadeDuration)
    {
        if (!IsServer)
        {
            if (_collider != null)
                _collider.enabled = false;
            StartCoroutine(DestructionFadeRoutine(fadeDuration));
        }
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

    private IEnumerator DestructionFadeRoutine(float duration)
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

    private IEnumerator DestructionFadeThenDespawn(float duration)
    {
        yield return DestructionFadeRoutine(duration);
        Despawn();
    }

    private void Despawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }

    public override void OnNetworkDespawn()
    {
        _despawned = true;
        StopAllCoroutines();
        base.OnNetworkDespawn();
    }
}
