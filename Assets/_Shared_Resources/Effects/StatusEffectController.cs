using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatusEffectController : NetworkBehaviour
{
    [System.Serializable]
    public class ActiveEffect
    {
        public Effect effect;
        public NetworkEntity source;
        public float remainingDuration;
        public float tickTimer;
    }

    private List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private NetworkEntity owner;
    private EnemyBrain ownerBrain;
    private EnemyMotor ownerMotor;
    public NetworkEntity Owner => owner;
    public EnemyBrain OwnerBrain => ownerBrain;
    public EnemyMotor OwnerMotor => ownerMotor;

    void Awake()
    {
        owner = GetComponent<NetworkEntity>();
        ownerBrain = GetComponent<EnemyBrain>();
        ownerMotor = GetComponent<EnemyMotor>();
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        TickEffects();
    }

    public void ApplyEffect(Effect effect, float duration, NetworkEntity source)
    {
        if (effect == null) return;
        // Remove existing effect of same kind (non-stacking)
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effect.kind == effect.kind)
            {
                activeEffects[i].effect.OnExpire(this);
                activeEffects.RemoveAt(i);
            }
        }

        ActiveEffect active = new ActiveEffect
        {
            effect = effect,
            source = source,
            remainingDuration = duration > 0 ? duration : effect.duration,
            tickTimer = 0f
        };

        activeEffects.Add(active);
        effect.OnApply(this);
    }

    public void RemoveEffect(EffectKind kind)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effect.kind == kind)
            {
                activeEffects[i].effect.OnExpire(this);
                activeEffects.RemoveAt(i);
                return;
            }
        }
    }

    public bool HasEffect(EffectKind kind)
    {
        foreach (var ae in activeEffects)
            if (ae.effect.kind == kind)
                return true;
        return false;
    }

    public void ClearAllEffects()
    {
        foreach (var ae in activeEffects)
            ae.effect.OnExpire(this);
        activeEffects.Clear();
    }

    void TickEffects()
    {
        float dt = Time.fixedDeltaTime;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect ae = activeEffects[i];
            ae.remainingDuration -= dt;
            if (ae.remainingDuration <= 0f)
            {
                ae.effect.OnExpire(this);
                activeEffects.RemoveAt(i);
                continue;
            }

            if (ae.effect.tickRate > 0f)
            {
                ae.tickTimer += dt;
                while (ae.tickTimer >= ae.effect.tickRate)
                {
                    ae.tickTimer -= ae.effect.tickRate;
                    ae.effect.OnTick(this);
                }
            }
        }
    }

    public float GetSpeedMultiplier()
    {
        float mult = 1f;
        foreach (var ae in activeEffects)
            mult *= ae.effect.speedMultiplier;
        return mult;
    }

    public bool IsMovementLocked()
    {
        foreach (var ae in activeEffects)
            if (ae.effect.stopsMovement) return true;
        return false;
    }

    public bool IsAttackLocked()
    {
        foreach (var ae in activeEffects)
            if (ae.effect.stopsAttack) return true;
        return false;
    }

    public NetworkEntity GetSource(EffectKind kind)
    {
        foreach (var ae in activeEffects)
            if (ae.effect.kind == kind)
                return ae.source;
        return null;
    }
}
