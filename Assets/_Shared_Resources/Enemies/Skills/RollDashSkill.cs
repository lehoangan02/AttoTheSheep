using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Skills/Roll Dash")]
public class RollDashSkill : EnemySkill
{
    public float dashSpeed = 20f;
    public float dashDuration = 1f;

    public override void Execute(EnemyBrain brain, Vector3 targetPosition)
    {
        brain.StartCoroutine(DashRoutine(brain, targetPosition));
    }

    System.Collections.IEnumerator DashRoutine(EnemyBrain brain, Vector3 targetPos)
    {
        // Warning phase (2s - the castTime)
        yield return new WaitForSeconds(castTime);

        // Dash phase
        Vector2 dashDir = (targetPos - brain.transform.position).normalized;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            if (brain.Motor != null)
                brain.Motor.MoveToward(brain.transform.position + (Vector3)dashDir * 10f, dashSpeed);
            yield return new WaitForFixedUpdate();
        }

        brain.Motor?.Stop();

        // Apply damage + effects to hit targets
        StatusEffectController controller = brain.GetComponent<StatusEffectController>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(brain.transform.position, aoeRadius);
        foreach (var hit in hits)
        {
            NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
            if (target == null || target == brain.Entity || !target.IsAlive) continue;

            target.TakeDamage(damage);

            if (onHitEffects != null && controller != null)
            {
                StatusEffectController targetController = target.GetComponent<StatusEffectController>();
                if (targetController != null)
                {
                    foreach (var effect in onHitEffects)
                        targetController.ApplyEffect(effect, effect.duration, brain.Entity);
                }
            }
        }
    }
}
