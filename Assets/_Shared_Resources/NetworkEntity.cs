using UnityEngine;
using Unity.Netcode;
using System;

// Common base class for all living entities in the game
public class NetworkEntity : NetworkBehaviour, IStatusTarget
{
    [Header("Base Entity Stats")]
    [SerializeField] protected float baseMoveSpeed = 5f;
    [SerializeField] protected int baseMaxHealth = 100;
    [SerializeField] protected int baseMaxMana = 100;

    [SerializeField] protected float baseAttackRange = 1f;
    [SerializeField] protected float baseAttackDamage = 10f;

    [Header("Damage Popup")]
    [SerializeField] protected TMPro.TMP_FontAsset damageFont;

    [Header("Status Effect VFX")]
    [Tooltip("Where status effect VFX should be parented. Defaults to this transform if unassigned.")]
    [SerializeField] private Transform vfxAnchor;

    /// <summary>Anchor for status effect VFX. Falls back to this transform.</summary>
    public Transform VfxAnchor => vfxAnchor != null ? vfxAnchor : transform;

    [HideInInspector] public StatusEffectController effectController;

    public NetworkVariable<float> currentMoveSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentMana = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isInvulnerable = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // Sync position and scale for multiplayer
    public NetworkVariable<Vector2> syncPosition = new NetworkVariable<Vector2>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> syncScaleX = new NetworkVariable<float>(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public event Action OnDied;

    protected void InvokeOnDied() => OnDied?.Invoke();

    public float BaseMoveSpeed => baseMoveSpeed;
    public float BaseAttackRange => baseAttackRange;
    public float BaseAttackDamage => baseAttackDamage;
    public int BaseMaxHealth => baseMaxHealth;
    public int BaseMaxMana => baseMaxMana;
    public bool IsAlive => currentHealth.Value > 0;

    public override void OnNetworkSpawn()
    {
        if (effectController == null)
            effectController = GetComponent<StatusEffectController>();

        if (IsServer)
        {
            // Initialize base stats on the Server
            currentMoveSpeed.Value = baseMoveSpeed;
            currentHealth.Value = baseMaxHealth;
            currentMana.Value = baseMaxMana;
        }
        else if (!IsOwner)
        {
            // Set Rigidbody to Kinematic on the client for non-owned objects so it doesn't fight transform sync
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        currentHealth.OnValueChanged += OnHealthChangedBase;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChangedBase;
    }

    private void OnHealthChangedBase(int previousValue, int newValue)
    {
        int damage = previousValue - newValue;
        if (damage > 0)
        {
            ShowDamagePopup(damage);
        }
    }

    private void ShowDamagePopup(int damage)
    {
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.position = transform.position + Vector3.up * 1.5f;
        DamagePopup dp = popup.AddComponent<DamagePopup>();
        bool isPlayer = CompareTag("Player");
        dp.Setup(damage, isPlayer, damageFont);
    }

    // Basic interaction functions (Virtual so child classes can override/modify)
    public virtual void TakeDamage(int damage)
    {
        if (!IsServer || currentHealth.Value <= 0) return;

        if (isInvulnerable.Value) return;

        int previousHealth = currentHealth.Value;
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);
        int actualDamage = previousHealth - currentHealth.Value;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    public virtual void TakeDamage(int damage, NetworkEntity source)
    {
        if (!IsServer || currentHealth.Value <= 0) return;
        if (isInvulnerable.Value) return;

        int previousHealth = currentHealth.Value;
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);
        int actualDamage = previousHealth - currentHealth.Value;

        string sourceName = (source != null) ? source.name : "Unknown";

        if (currentHealth.Value <= 0)
            Die();
    }
    // Mana deduction function for casting skills (Only Server can deduct)
    public virtual bool ConsumeMana(int amount)
    {
        if (!IsServer) return false;

        // If enough mana, deduct and allow casting skill (return true)
        if (currentMana.Value >= amount)
        {
            currentMana.Value -= amount;
            return true;
        }

        // If not enough mana, return false
        return false;
    }

    // Mana restoration function (used for potions or auto-regen later)
    public virtual void RestoreMana(int amount)
    {
        if (!IsServer || currentHealth.Value <= 0) return;
        currentMana.Value = Mathf.Min(baseMaxMana, currentMana.Value + amount);
    }

    public virtual void Heal(int amount)
    {
        if (!IsServer || currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Min(baseMaxHealth, currentHealth.Value + amount);
    }

    public virtual void ApplyKnockback(Vector2 force, float duration)
    {
        if (!IsServer) return;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        StartCoroutine(KnockbackRoutine(rb, force, duration));
    }

    System.Collections.IEnumerator KnockbackRoutine(Rigidbody2D rb, Vector2 force, float duration)
    {
        EnemyBrain brain = GetComponent<EnemyBrain>();

        // Disable AI during knockback
        if (brain != null) brain.IsFrozen = true;

        rb.linearVelocity = force;

        yield return new WaitForSeconds(duration);

        if (brain != null) brain.IsFrozen = false;
    }

    [ClientRpc]
    protected virtual void HandleDeathClientRpc()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (damageFont == null)
        {
            damageFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Fredoka-Bold SDF.asset");
        }
    }
#endif

    protected virtual void Die()
    {
        OnDied?.Invoke();
        // Default behavior: destroy entity on network when dead
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    protected virtual void Update()
    {
        if (IsServer)
        {
            syncPosition.Value = transform.position;
            syncScaleX.Value = transform.localScale.x;
        }
        else if (IsClient && !IsOwner)
        {
            if (Vector2.Distance(transform.position, syncPosition.Value) > 0.001f)
            {
                transform.position = Vector2.Lerp(transform.position, syncPosition.Value, Time.deltaTime * 15f);
            }
            Vector3 scale = transform.localScale;
            scale.x = syncScaleX.Value;
            transform.localScale = scale;
        }
    }
}