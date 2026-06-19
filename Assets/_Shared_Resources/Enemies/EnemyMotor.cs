using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor : MonoBehaviour
{
    Rigidbody2D rb;
    public float speedMultiplier = 1f;
    public bool IsFrozen { get; set; }
    public bool IsMoving { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void MoveToward(Vector3 target, float baseSpeed)
    {
        if (IsFrozen) { Stop(); return; }
        if (rb == null) return;

        Vector2 direction = (target - transform.position).normalized;
        rb.linearVelocity = direction * baseSpeed * speedMultiplier;
        IsMoving = true;

        Vector3 scale = transform.localScale;
        if (direction.x > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        else if (direction.x < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
    }

    public void Stop()
    {
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.Sleep(); }
        IsMoving = false;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    System.Collections.IEnumerator KnockbackRoutine(Vector2 force, float duration)
    {
        if (rb != null) rb.linearVelocity = force;
        IsFrozen = true;
        yield return new WaitForSeconds(duration);
        IsFrozen = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void DashToward(Vector3 target, float dashSpeed, float duration)
    {
        StartCoroutine(DashRoutine(target, dashSpeed, duration));
    }

    System.Collections.IEnumerator DashRoutine(Vector3 target, float dashSpeed, float duration)
    {
        float elapsed = 0f;
        Vector2 dir = (target - transform.position).normalized;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            if (rb != null) rb.linearVelocity = dir * dashSpeed;
            yield return new WaitForFixedUpdate();
        }
        Stop();
    }
}
