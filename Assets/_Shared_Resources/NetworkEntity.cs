using UnityEngine;
using Unity.Netcode;
using System;

// Common base class for all living entities in the game
public class NetworkEntity : NetworkBehaviour
{
    [Header("Base Entity Stats")]
    [SerializeField] protected float baseMoveSpeed = 5f;
    [SerializeField] protected int baseMaxHealth = 100;
    [SerializeField] protected int baseMaxMana = 100;

    [SerializeField] protected float baseAttackRange = 1f;
    [SerializeField] protected float baseAttackDamage = 10f;



    // Biến mạng đồng bộ cho mọi người chơi thấy
    public NetworkVariable<float> currentMoveSpeed = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> currentMana = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public event Action OnDied;

    public float BaseMoveSpeed => baseMoveSpeed;
    public float BaseAttackRange => baseAttackRange;
    public float BaseAttackDamage => baseAttackDamage;
    public int BaseMaxHealth => baseMaxHealth;
    public int BaseMaxMana => baseMaxMana;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize base stats on the Server
            currentMoveSpeed.Value = baseMoveSpeed;
            currentHealth.Value = baseMaxHealth;
            currentMana.Value = baseMaxMana;
        }
    }

    // Basic interaction functions (Virtual so child classes can override/modify)
    public virtual void TakeDamage(int damage)
    {
        if (!IsServer || currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
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

    protected virtual void Die()
    {
        OnDied?.Invoke();
        // Default behavior: destroy entity on network when dead
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}