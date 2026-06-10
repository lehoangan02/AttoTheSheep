using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void MoveToward(Vector2 targetPosition, float speed)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * speed;
        if (spriteRenderer != null)
        {
            if (direction.x > 0) spriteRenderer.flipX = false;
            else if (direction.x < 0) spriteRenderer.flipX = true;
        }
    }

    public void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }
}
