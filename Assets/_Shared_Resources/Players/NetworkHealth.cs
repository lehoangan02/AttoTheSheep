using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkHealth : NetworkBehaviour
{
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private int maxHealth = 100;

    // Interface/Event cho các hệ thống UI cắm vào
    public event Action<int, int> OnHealthChangedInterface;

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthVariableChanged;
        // Cập nhật UI phát đầu tiên
        OnHealthChangedInterface?.Invoke(currentHealth.Value, maxHealth);
    }

    private void OnHealthVariableChanged(int oldVal, int newVal)
    {
        OnHealthChangedInterface?.Invoke(newVal, maxHealth);
    }

    // Hàm gọi từ Server khi dính chiêu từ PlayerSkills của đối thủ
    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damageAmount);
        Debug.Log($"[SERVER] Player {OwnerClientId} trúng {damageAmount} sát thương. Máu còn: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Debug.Log($"[SERVER] Player {OwnerClientId} ĐÃ CHẾT!");
        }
    }
    // Hàm gọi từ Server để hồi máu (Do PlayerSynergy yêu cầu)
    public void Heal(int healAmount)
    {
        if (!IsServer) return; // Chỉ Server mới được phép đổi máu

        // Cộng máu nhưng không được vượt quá maxHealth
        currentHealth.Value = Mathf.Min(maxHealth, currentHealth.Value + healAmount);
        Debug.Log($"[SERVER] Player {OwnerClientId} được hồi {healAmount} máu. Máu hiện tại: {currentHealth.Value}");
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthVariableChanged;
    }
}