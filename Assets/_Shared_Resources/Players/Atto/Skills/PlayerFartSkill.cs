using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine; // THÊM THƯ VIỆN NÀY

public class PlayerFartSkill : BaseSkillComponent
{
    [Header("References")]
    [SerializeField] private List<TrailRenderer> dashTrails;
    [SerializeField] private Collider2D parentCollider; 
    [SerializeField] private Rigidbody2D parentRb;
    [SerializeField] private SpriteRenderer playerSprite; 

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource impulseSource; // THÊM BIẾN NÀY

    private FartSkillData currentFartData;
    private PlayerController fartController;
    private bool isDashing = false; 
    private List<Vector3> originalTrailLocalPositions;

    private void Start()
    {
        if (dashTrails == null || dashTrails.Count == 0)
        {
            dashTrails = new List<TrailRenderer>(GetComponentsInChildren<TrailRenderer>(true));
        }

        originalTrailLocalPositions = new List<Vector3>();
        foreach (var trail in dashTrails)
        {
            if (trail != null)
            {
                originalTrailLocalPositions.Add(trail.transform.localPosition);
                trail.emitting = false; 
            }
            else
            {
                originalTrailLocalPositions.Add(Vector3.zero);
            }
        }
    }

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (data is FartSkillData fartData && controller != null)
        {
            currentFartData = fartData;
            fartController = controller;
            StartCoroutine(DashAndDamageRoutine(fartData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {        
        base.ClientPlayVisual(data);
        if (data is FartSkillData fartData)
        {
            StartCoroutine(PlayTrailRoutine(fartData.dashDuration));
            StartCoroutine(LockMovementOnClientRoutine(fartData.dashDuration));

            if (IsOwner)
            {
                StartCoroutine(OwnerDashMovementRoutine(fartData));
            }
        }
    }

    private IEnumerator OwnerDashMovementRoutine(FartSkillData data)
    {
        Rigidbody2D parentRb = transform.root.GetComponent<Rigidbody2D>();
        Collider2D parentCollider = transform.root.GetComponent<Collider2D>();

        if (parentRb == null || parentCollider == null) yield break;

        Vector2 dashDir = transform.root.GetComponentInChildren<SpriteRenderer>().flipX ? Vector2.left : Vector2.right;
        
        if (parentRb.linearVelocity.magnitude > 0.1f)
        {
            dashDir = parentRb.linearVelocity.normalized;
        }

        parentRb.linearVelocity = dashDir * data.dashForce;

        float elapsed = 0f;
        bool hasCollided = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(data.enemyLayer | LayerMask.GetMask("Ground", "Wall"));
        filter.useLayerMask = true;

        RaycastHit2D[] hits = new RaycastHit2D[1];

        while (elapsed < data.dashDuration && !hasCollided)
        {
            parentRb.linearVelocity = dashDir * data.dashForce;

            float moveDistance = data.dashForce * Time.fixedDeltaTime;
            int hitCount = parentCollider.Cast(dashDir, filter, hits, moveDistance + 0.1f);

            if (hitCount > 0)
            {
                hasCollided = true; 
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        parentRb.linearVelocity = Vector2.zero;
    }

    private IEnumerator LockMovementOnClientRoutine(float duration)
    {
        PlayerMovement movement = transform.root.GetComponentInChildren<PlayerMovement>();
        if (movement != null) movement.isMovementLocked = true;
        
        yield return new WaitForSeconds(duration);
        
        if (movement != null) movement.isMovementLocked = false;
    }

    private IEnumerator PlayTrailRoutine(float duration)
    {
        if (dashTrails == null || dashTrails.Count == 0) yield break;

        bool flip = playerSprite != null && playerSprite.flipX;

        for (int i = 0; i < dashTrails.Count; i++)
        {
            if (dashTrails[i] == null) continue;

            Vector3 originalPos = (originalTrailLocalPositions != null && originalTrailLocalPositions.Count > i)
                ? originalTrailLocalPositions[i]
                : dashTrails[i].transform.localPosition;

            float targetX = flip ? -originalPos.x : originalPos.x;
            dashTrails[i].transform.localPosition = new Vector3(targetX, originalPos.y, originalPos.z);
            dashTrails[i].emitting = true;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < dashTrails.Count; i++)
        {
            if (dashTrails[i] != null)
            {
                dashTrails[i].emitting = false;
            }
        }
    }

    private IEnumerator DashAndDamageRoutine(FartSkillData data, PlayerController controller)
    {
        if (parentRb == null || parentCollider == null)
        {
            Debug.LogError("❌ [LOGIC LỖI] Bạn chưa kéo thả Rigidbody hoặc Collider của Object Cha vào Inspector!");
            yield break;
        }

        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();

        float damageMultiplier = 1f;
        PlayerSkills skills = controller.GetComponentInChildren<PlayerSkills>();
        if (skills == null) skills = controller.GetComponentInParent<PlayerSkills>();
        if (skills != null) damageMultiplier = skills.damageMultiplier.Value;

        Vector2 dashDir = parentRb.linearVelocity.normalized;
        if (dashDir == Vector2.zero)
        {
            dashDir = (playerSprite != null && playerSprite.flipX) ? Vector2.left : Vector2.right;
        }

        if (movement != null) movement.isMovementLocked = true;

        parentCollider.isTrigger = false;

        float elapsed = 0f;
        bool hasCollided = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(data.enemyLayer | LayerMask.GetMask("Ground", "Wall"));
        filter.useLayerMask = true;

        RaycastHit2D[] hits = new RaycastHit2D[1];

        while (elapsed < data.dashDuration && !hasCollided)
        {
            parentRb.linearVelocity = dashDir * data.dashForce;

            float moveDistance = data.dashForce * Time.fixedDeltaTime;

            int hitCount = parentCollider.Cast(dashDir, filter, hits, moveDistance + 0.1f);

            if (hitCount > 0)
            {
                RaycastHit2D hit = hits[0]; 
                Debug.Log($"🛑 [PHYSICS CAST] Tông trúng: {hit.collider.gameObject.name}. Dừng lướt!");

                hasCollided = true; 

                if (skills != null)
                {
                    skills.PlaySkillHitVisualClientRpc(data.skillId, hit.point);
                }

                if (((1 << hit.collider.gameObject.layer) & data.enemyLayer) != 0)
                {
                    NetworkEntity enemyEntity = hit.collider.GetComponent<NetworkEntity>();
                    if (enemyEntity != null)
                    {
                        int finalDamage = Mathf.RoundToInt(data.damage * damageMultiplier);
                        enemyEntity.TakeDamage(finalDamage);
                        Vector2 knockbackDir = ((Vector2)hit.collider.transform.position - (Vector2)parentCollider.bounds.center).normalized;
                        enemyEntity.ApplyKnockback(knockbackDir * data.knockupForce, 0.3f);
                    }
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        parentRb.linearVelocity = Vector2.zero;
        if (movement != null) movement.isMovementLocked = false;

        currentFartData = null;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing || currentFartData == null || fartController == null) return;

        Debug.Log($"🛑 [PHYSICS] Tông trúng Collider: {collision.gameObject.name}. Dừng lướt ngay lập tức!");
        
        isDashing = false; 
        Rigidbody2D rb = fartController.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (((1 << collision.gameObject.layer) & currentFartData.enemyLayer) != 0)
        {
            Debug.Log($"💥 [PHYSICS] Xác nhận mục tiêu là Enemy: {collision.gameObject.name}. Kích hoạt Knockback!");
            
            NetworkEntity enemyEntity = collision.gameObject.GetComponent<NetworkEntity>();
            if (enemyEntity != null)
            {
                float damageMultiplier = 1f;
                if (fartController != null)
                {
                    PlayerSkills skills = fartController.GetComponentInChildren<PlayerSkills>();
                    if (skills == null) skills = fartController.GetComponentInParent<PlayerSkills>();
                    if (skills != null) damageMultiplier = skills.damageMultiplier.Value;
                    
                    if (skills != null && collision.contactCount > 0)
                    {
                        skills.PlaySkillHitVisualClientRpc(currentFartData.skillId, collision.GetContact(0).point);
                    }
                }
                int finalDamage = Mathf.RoundToInt(currentFartData.damage * damageMultiplier);
                enemyEntity.TakeDamage(finalDamage);

                float dirX = collision.transform.position.x > fartController.transform.position.x ? 1f : -1f;
                Vector2 knockbackDir = new Vector2(dirX, 0f).normalized; 
                
                enemyEntity.ApplyKnockback(knockbackDir * currentFartData.knockupForce, 0.3f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f); 
    }

    public override void ClientPlayHitEffect(SkillData data, Vector2 hitPosition)
    {
        base.ClientPlayHitEffect(data, hitPosition);
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }
}