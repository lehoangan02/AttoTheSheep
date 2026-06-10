using UnityEngine;

public class CasterEnemyAI : EnemyAI
{
    [Header("Caster")]
    [SerializeField] private float fireCooldown = 10f;
    [SerializeField] private float iceCooldown = 10f;
    [SerializeField] private float initialIceDelay = 5f;

    private float fireTimer;
    private float iceTimer;

    protected override void Awake()
    {
        base.Awake();
        fireTimer = fireCooldown;
        iceTimer = Mathf.Max(0f, iceCooldown - initialIceDelay);
    }

    protected virtual void Update()
    {
        if (!CanRunAI || currentState == State.Casting) return;

        fireTimer += Time.deltaTime;
        iceTimer += Time.deltaTime;
    }

    protected override void ProcessAttack(float distance)
    {
        EnemyData data = Data;
        float attackRange = data != null ? data.attackRange : entity.AttackRange;

        movement?.Stop();

        if (distance > attackRange)
        {
            currentState = State.Chase;
            return;
        }

        if (fireTimer >= fireCooldown)
        {
            CastSpell("CastFire");
            fireTimer = 0f;
        }
        else if (iceTimer >= iceCooldown)
        {
            CastSpell("CastIce");
            iceTimer = 0f;
        }
    }

    private void CastSpell(string animationTrigger)
    {
        currentState = State.Casting;
        movement?.Stop();
        SetAnimatorTrigger(animationTrigger);
    }

    public void FinishCasting()
    {
        currentState = State.Idle;
    }
}
