using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Skills/Tornado")]
public class TornadoSkill : EnemySkill
{
    public override void Execute(EnemyBrain brain, Vector3 targetPosition)
    {
        // Speed boost
        if (brain.Motor != null)
            brain.Motor.SetSpeedMultiplier(2f);

        // Start tornado coroutine on brain
        brain.StartCoroutine(TornadoRoutine(brain));
    }

    System.Collections.IEnumerator TornadoRoutine(EnemyBrain brain)
    {
        float elapsed = 0f;
        float tornadoDuration = 5f;
        float tickRate = 0.5f; // damage every 0.5s

        while (elapsed < tornadoDuration)
        {
            elapsed += tickRate;

            // Damage nearby enemies (or in this case, targets)
            Collider2D[] hits = Physics2D.OverlapCircleAll(brain.transform.position, aoeRadius);
            foreach (var hit in hits)
            {
                NetworkEntity target = hit.GetComponentInParent<NetworkEntity>();
                if (target == null || target == brain.Entity || !target.IsAlive) continue;
                target.TakeDamage(Mathf.RoundToInt(damage * tickRate));
            }

            yield return new WaitForSeconds(tickRate);
        }

        // Reset speed
        if (brain.Motor != null)
            brain.Motor.SetSpeedMultiplier(1f);
    }
}
