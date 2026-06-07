using UnityEngine;
using Unity.Netcode;
using System;

// Lớp cha dùng chung cho mọi thực thể sống trong game
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Khởi tạo chỉ số gốc trên Server
            currentMoveSpeed.Value = baseMoveSpeed;
            currentHealth.Value = baseMaxHealth;
        }
    }

    // Các hàm tương tác cơ bản (Virtual để các class con có thể ghi đè/thay đổi)
    public virtual void TakeDamage(int damage)
    {
        if (!IsServer || currentHealth.Value <= 0) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }
    // Hàm trừ Mana khi tung chiêu (Chỉ Server được trừ)
    public virtual bool ConsumeMana(int amount)
    {
        if (!IsServer) return false;

        // Nếu đủ mana thì trừ và cho phép tung chiêu (trả về true)
        if (currentMana.Value >= amount)
        {
            currentMana.Value -= amount;
            return true;
        }
        
        // Nếu không đủ mana thì báo false
        return false;
    }

    // Hàm hồi Mana (dùng cho bình thuốc hoặc tự hồi phục sau này)
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
        // Mặc định entity chết thì hủy trên mạng
        if (NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}