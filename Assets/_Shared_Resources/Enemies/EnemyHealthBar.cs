using UnityEngine;
using Unity.Netcode;

// Bridges NetworkEntity.currentHealth to a HealthBar view component.
// Attach to any enemy prefab that has a NetworkEntity. Drop a HealthBar reference
// in the inspector; if none is provided, this component is a no-op.
[RequireComponent(typeof(NetworkEntity))]
public class EnemyHealthBar : NetworkBehaviour
{
    [SerializeField] private HealthBar healthBar;

    private NetworkEntity entity;

    public override void OnNetworkSpawn()
    {
        entity = GetComponent<NetworkEntity>();
        if (entity == null) return;

        // Initial paint — NetworkVariable is synced by the time OnNetworkSpawn fires.
        if (healthBar != null)
        {
            healthBar.SetHealth(entity.currentHealth.Value, entity.BaseMaxHealth);
        }

        entity.currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnDestroy()
    {
        // Despawn(true) destroys the GameObject, so OnDestroy is the safe teardown point.
        if (entity != null)
        {
            entity.currentHealth.OnValueChanged -= OnHealthChanged;
        }

        base.OnDestroy();
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(newValue, entity.BaseMaxHealth);
        }
    }
}
