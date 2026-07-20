using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections;

public class PlayerEntity : NetworkEntity
{

    public static event System.Action OnAnyPlayerDied;

    [Header("Damage Feedback (Hiệu ứng trúng đòn)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private Color originalColor;
    private Rigidbody2D rb;
    private PlayerAudio playerAudio;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        playerMovement = GetComponentInChildren<PlayerMovement>();
        playerAudio = GetComponent<PlayerAudio>();
        if (playerAudio == null) playerAudio = gameObject.AddComponent<PlayerAudio>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {

            SetupVirtualCamera();
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    private float _multiplayerRegenTimer = 0f;

    private void Update()
    {
        if (IsServer && IsAlive)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MultiplayerLevel")
            {
                _multiplayerRegenTimer += Time.deltaTime;
                if (_multiplayerRegenTimer >= 2f) // Every 2 seconds
                {
                    _multiplayerRegenTimer -= 2f;
                    if (currentHealth.Value < baseMaxHealth)
                    {
                        currentHealth.Value = Mathf.Min(baseMaxHealth, currentHealth.Value + 3); // Recover 3 HP
                    }
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void SetupVirtualCamera()
    {
        CinemachineCamera vCam = FindAnyObjectByType<CinemachineCamera>();

        if (vCam != null)
        {
            vCam.Follow = this.transform;

            // Fix: Disable camera Lookahead to prevent violent camera warping during dashes.
            var composer = vCam.GetComponent<Unity.Cinemachine.CinemachinePositionComposer>();
            if (composer != null)
            {
                // When dash applies high velocity, Lookahead extrapolates it and jerks the camera.
                composer.Lookahead.Enabled = false;

                // Tighten damping slightly to keep the camera focused on the player without sluggishness
                composer.Damping = new Vector3(1f, 1f, 1f);
            }

        }
        else
        {

        }
    }

    protected override void Die()
    {
        PlayDeathClientRpc();
        base.Die();

        OnAnyPlayerDied?.Invoke();
    }

    // ==========================================

    // ==========================================
    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (newValue < previousValue)
        {
            if (spriteRenderer != null)
            {
                StopCoroutine(nameof(FlashRedRoutine));
                StartCoroutine(FlashRedRoutine());
            }

            if (newValue > 0)
            {
                playerAudio?.PlayHurt();
            }
        }
    }

    [ClientRpc]
    private void PlayDeathClientRpc()
    {
        playerAudio?.PlayDeath();
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    // ==========================================

    // ==========================================
    public override void TakeDamage(int damage, NetworkEntity source)
    {
        int healthBefore = currentHealth.Value;

        base.TakeDamage(damage, source);

        if (IsServer && currentHealth.Value < healthBefore && source != null)
        {
            Vector2 knockbackDirection = (transform.position - source.transform.position).normalized;
            Vector2 appliedForce = knockbackDirection * knockbackForce;

            ApplyPlayerKnockbackClientRpc(appliedForce);
        }
    }

    [ClientRpc]
    private void ApplyPlayerKnockbackClientRpc(Vector2 force)
    {
        if (rb != null)
        {
            StartCoroutine(PlayerKnockbackRoutine(force));
        }
    }

    private IEnumerator PlayerKnockbackRoutine(Vector2 force)
    {

        if (playerMovement != null) playerMovement.isMovementLocked = true;

        rb.linearVelocity = force;

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;
        if (playerMovement != null) playerMovement.isMovementLocked = false;
    }
}
