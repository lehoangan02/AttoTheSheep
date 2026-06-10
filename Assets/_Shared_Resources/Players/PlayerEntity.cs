using UnityEngine;
using Unity.Netcode;

// INHERIT FROM COMMON CLASS: Has all health and speed variables from NetworkEntity
public class PlayerEntity : NetworkEntity
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Still calls the parent class's base stat setup
        
        if (IsOwner)
        {
            Debug.Log("Player Entity has been spawned!");
        }
    }

    // OVERRIDE PARENT CLASS: Show Game Over when Player dies instead of deleting the object
    protected override void Die()
    {
        base.Die();
        Debug.Log("Player has been defeated! Showing Game Over screen...");
        // Logic for reviving, deducting coins, etc.
    }
}