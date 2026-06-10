using UnityEngine;

public class BossEnemyAI : EnemyAI
{
    [Header("Boss Cooldowns")]
    [SerializeField] private float smashCooldown = 15f;
    [SerializeField] private float tornadoCooldown = 20f;
    [SerializeField] private float dashCooldown = 25f;

    private float smashTimer;
    private float tornadoTimer;
    private float dashTimer;

    protected virtual void Update()
    {
        if (!CanRunAI || currentState == State.Casting) return;

        smashTimer += Time.deltaTime;
        tornadoTimer += Time.deltaTime;
        dashTimer += Time.deltaTime;
    }

    protected override void ProcessAttack(float distance)
    {
        EnemyData data = Data;
        float attackRange = data != null ? data.attackRange : entity.AttackRange;

        movement?.Stop();

        if (dashTimer >= dashCooldown)
        {
            StartSkill("RollDash");
            dashTimer = 0f;
        }
        else if (tornadoTimer >= tornadoCooldown)
        {
            StartSkill("Tornado");
            tornadoTimer = 0f;
        }
        else if (smashTimer >= smashCooldown && distance <= attackRange)
        {
            StartSkill("Smash");
            smashTimer = 0f;
        }
        else if (distance > attackRange)
        {
            currentState = State.Chase;
        }
    }

    private void StartSkill(string skillTrigger)
    {
        currentState = State.Casting;
        movement?.Stop();
        SetAnimatorTrigger(skillTrigger);
    }

    public void EndSkill()
    {
        currentState = State.Idle;
    }
}
