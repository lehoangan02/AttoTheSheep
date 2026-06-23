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
        // Find the stat core for healing
        entity = GetComponentInParent<NetworkEntity>();
        
        // Find the Skill list on the same Object to unlock skills
        skills = GetComponent<PlayerSkills>(); 
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
                myFlock = FindFirstObjectByType<FlockManager>();

            if (myFlock != null)
            {
                // Start the flock checking loop (Defaults to once per second)
                InvokeRepeating(nameof(CheckFlockBuffs), buffCheckInterval, buffCheckInterval);
            }
            else
            {
                // [MULTIPLAYER MODE] If there are no lambs on the map, automatically unlock all skills (Tier 10)
                UpdateSkillTierServerRpc(10);
            }
        }
    }

    private void CheckFlockBuffs()
    {
        if (myFlock == null) return;
        
        // Check if the player is standing inside any flock zone
        Vector3 parentPos = transform.parent != null ? transform.parent.position : transform.position;
        bool isInsideHealZone = myFlock.IsPositionInsideHealZone(parentPos);
        bool isInsideSkillZone = myFlock.IsPositionInsideSkillZone(parentPos);
        
        // Send information to the Server
        UpdateBuffsServerRpc(myFlock.activeLambs.Count, myFlock.GetFlockTier(), isInsideHealZone, isInsideSkillZone, myFlock.HealScale, myFlock.ManaScale);
    }

    [ServerRpc]
    private void UpdateBuffsServerRpc(int clientFlockSize, int flockTier, bool isInsideHealZone, bool isInsideSkillZone, float healScale, float manaScale)
    {
        if (skills == null) return;

        // 0. UPDATE "is inside skill zone" status for PlayerSkills
        skills.isInsideFlock.Value = isInsideSkillZone;

        // 1. UNLOCK SKILLS
        skills.unlockedSkillTier.Value = flockTier;

        // 2. RESTORE: Only heal when standing inside the flock's heal zone
        if (isInsideHealZone && clientFlockSize > 0 && entity != null)
        {
            // Heal if not full
            if (entity.currentHealth.Value < entity.BaseMaxHealth)
            {
                int healAmount = Mathf.RoundToInt(healScale * clientFlockSize);
                entity.Heal(Mathf.Max(1, healAmount));
            }

            // Restore mana if not full
            if (entity.currentMana.Value < entity.BaseMaxMana)
            {
                int manaAmount = Mathf.RoundToInt(manaScale * clientFlockSize);
                entity.RestoreMana(Mathf.Max(1, manaAmount));
            }
        }
    }

    // Helper function for no-flock mode
    [ServerRpc]
    private void UpdateSkillTierServerRpc(int tier)
    {
        if (skills != null)
        {
            skills.unlockedSkillTier.Value = tier;
            skills.isInsideFlock.Value = true;
        }
    }
}