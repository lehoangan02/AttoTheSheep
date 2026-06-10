using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkHealth : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private int maxHealth = 100;

    // Interface/Event for UI systems to plug into
    public event Action<int, int> OnHealthChangedInterface;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthVariableChanged;
        // Initial UI update
        OnHealthChangedInterface?.Invoke(currentHealth.Value, maxHealth);
    }

    private void OnHealthVariableChanged(int oldVal, int newVal)
    {
        OnHealthChangedInterface?.Invoke(newVal, maxHealth);
    }

    // Function called from Server when hit by opponent's PlayerSkills
    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damageAmount);
        Debug.Log($"[SERVER] Player {OwnerClientId} took {damageAmount} damage. Remaining HP: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Debug.Log($"[SERVER] Player {OwnerClientId} HAS DIED!");
        }
    }
    // Function called from Server to heal (Requested by PlayerFlockBuffs)
    public void Heal(int healAmount)
    {
        if (!IsServer) return; // Only Server is allowed to change health

        // Add health but do not exceed maxHealth
        currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + healAmount);
        Debug.Log($"[SERVER] Player {OwnerClientId} was healed for {healAmount} HP. Current HP: {currentHealth.Value}");
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthVariableChanged;
    }
}