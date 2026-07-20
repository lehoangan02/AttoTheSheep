using UnityEngine;
using Unity.Netcode;

public class PlayerFlockBuffs : NetworkBehaviour
{
    private FlockManager myFlock;
    private NetworkEntity entity;
    private PlayerSkills skills;

    [SerializeField] private float buffCheckInterval = 1f;
    [SerializeField] private int baseHealAmount = 5;

    void Awake()
    {
        entity = GetComponentInParent<NetworkEntity>();
        skills = GetComponent<PlayerSkills>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // We find the correct FlockManager for this player
            var flocks = FindObjectsByType<FlockManager>(FindObjectsSortMode.None);
            foreach (var fm in flocks)
            {
                if (fm.OwnerClientId == OwnerClientId || flocks.Length == 1)
                {
                    myFlock = fm;
                    break;
                }
            }

            if (myFlock != null)
            {

                InvokeRepeating(nameof(CheckFlockBuffs), buffCheckInterval, buffCheckInterval);
            }
            else
            {
                // If it's null on spawn, we will try to find it in the first CheckFlockBuffs call
                InvokeRepeating(nameof(CheckFlockBuffs), buffCheckInterval, buffCheckInterval);
            }
        }
    }

    private void CheckFlockBuffs()
    {
        if (myFlock == null)
        {
            var flocks = FindObjectsByType<FlockManager>(FindObjectsSortMode.None);
            foreach (var fm in flocks)
            {
                if (fm.OwnerClientId == OwnerClientId || flocks.Length == 1)
                {
                    myFlock = fm;
                    break;
                }
            }

            if (myFlock == null)
            {

                UpdateSkillLambsServerRpc(999);
                return;
            }
        }

        Vector3 parentPos = transform.parent != null ? transform.parent.position : transform.position;
        bool isInsideHealZone = myFlock.IsPositionInsideHealZone(parentPos);

        UpdateBuffsServerRpc(myFlock.activeLambs.Count, isInsideHealZone, myFlock.HealScale, myFlock.ManaScale);
    }

    [ServerRpc]
    private void UpdateBuffsServerRpc(int clientFlockSize, bool isInsideHealZone, float healScale, float manaScale)
    {

        if (isInsideHealZone && clientFlockSize > 0 && entity != null)
        {
            if (entity.currentHealth.Value < entity.BaseMaxHealth)
            {
                int healAmount = Mathf.RoundToInt(healScale * clientFlockSize);
                entity.Heal(Mathf.Max(1, healAmount));
            }

            if (entity.currentMana.Value < entity.BaseMaxMana)
            {
                int manaAmount = Mathf.RoundToInt(manaScale * clientFlockSize);
                entity.RestoreMana(Mathf.Max(1, manaAmount));
            }
        }
    }

    [ServerRpc]
    private void UpdateSkillLambsServerRpc(int simulatedLambCount)
    {
        if (skills != null)
        {

            skills.currentLambCount.Value = simulatedLambCount;
            skills.isInsideFlock.Value = true;
        }
    }
}