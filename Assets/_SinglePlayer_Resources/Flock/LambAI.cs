using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkEntity))] // Lambs still use the common OOP core for Health/Speed
public class LambAI : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 0.1f; 
    [SerializeField] private float slowingRadius = 2f; 
    [SerializeField] private float accelerationRate = 5f; 

    [Header("Noise Settings")]
    [Range(0f, 0.9f)]
    [SerializeField] private float speedNoiseRange = 0.25f;

    private float personalSpeedMultiplier = 1f; 

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private FlockManager myManager;
    private NetworkEntity entity; 

    private Vector2 flockCenter;
    private float flockRadius;
    private Vector2 localOffset; 
    private bool hasTarget = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        entity = GetComponent<NetworkEntity>(); 
    }

    void Start()
    {
        personalSpeedMultiplier = Random.Range(1f - speedNoiseRange, 1f + speedNoiseRange);
    }

    public void Initialize(FlockManager manager)
    {
        myManager = manager;
    }

    public override void OnNetworkSpawn()
    {
        // Register event: When Entity reports health = 0 -> Remove from flock
        entity.OnDied += HandleLambDeath;
    }

    public void SetFlockData(Vector2 center, float radius)
    {
        flockCenter = center;
        flockRadius = radius;
        hasTarget = true;
        PickNewOffset(); 
    }

    private void PickNewOffset()
    {
        localOffset = Random.insideUnitCircle * flockRadius;
    }

    void FixedUpdate()
    {
        // Safety check: If not spawned or not the server, do not calculate
        if (!IsSpawned || !IsServer) return; 

        if (!hasTarget) 
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimationLocal(rb.linearVelocity.magnitude > 0.1f);
            return;
        }

        Vector2 actualTarget = flockCenter + localOffset;
        float distToTarget = Vector2.Distance(transform.position, actualTarget);
        
        if (distToTarget > stoppingDistance)
        {
            Vector2 direction = (actualTarget - (Vector2)transform.position).normalized;
            
            // READ STATS: Get speed from the OOP core NetworkEntity
            float maxSpeedWithNoise = entity.currentMoveSpeed.Value * personalSpeedMultiplier;
            float targetSpeed = maxSpeedWithNoise;
            
            if (distToTarget < slowingRadius)
            {
                targetSpeed = maxSpeedWithNoise * (distToTarget / slowingRadius);
            }

            Vector2 desiredVelocity = direction * targetSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, accelerationRate * Time.fixedDeltaTime);
            UpdateAnimationLocal(true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            hasTarget = false; 
            UpdateAnimationLocal(false);
        }
    }

    // ==========================================
    // LOCAL FUNCTION: NO NEED TO USE RPC
    // ==========================================
    private void UpdateAnimationLocal(bool isMoving)
    {
        if (animator != null) animator.SetBool("IsMoving", isMoving);

        if (spriteRenderer != null)
        {
            if (rb.linearVelocity.x > 0.01f) spriteRenderer.flipX = false;
            else if (rb.linearVelocity.x < -0.01f) spriteRenderer.flipX = true;
        }
    }

    private void HandleLambDeath()
    {
        if (myManager != null) 
        {
            myManager.RemoveLamb(this);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;

        // OOP Mechanism: Touching an enemy causes health loss (instead of instant death)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Deduct health through the parent class NetworkEntity's function
            // You can pass the Enemy's damage in, here for example it is 20 damage
            entity.TakeDamage(20); 
        }
    }

    public override void OnNetworkDespawn()
    {
        if (entity != null) entity.OnDied -= HandleLambDeath;
    }
}