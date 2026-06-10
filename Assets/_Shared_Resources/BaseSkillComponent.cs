using UnityEngine;
using Unity.Netcode;

// Abstract class defining "All Skills must have these 2 functions"
public abstract class BaseSkillComponent : NetworkBehaviour
{
    // 1. Function executed on Server (Calculate damage, physics, deduct health)
    // Pass in NetworkEntity to know who cast the skill (Atto, Monster, or Boss)
    public abstract void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null);

    // 2. Function executed on Client (Play Particle, Animation, Audio)
    public abstract void ClientPlayVisual(SkillData data);
}