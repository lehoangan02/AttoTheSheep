using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Cinemachine; // THÊM THƯ VIỆN NÀY

public class PlayerHeadbuttSkill : BaseSkillComponent
{
    [Header("References - Set in Inspector")]
    [SerializeField] private Animator animator;          
    [SerializeField] private LayerMask enemyLayer;       
    
    [SerializeField] private SpriteRenderer playerSprite; 
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem impactParticle; 

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource impulseSource; // THÊM BIẾN NÀY

    private HeadbuttSkillData currentHeadbuttData;
    
    private float nextAttackTimeServer = 0f;
    private float nextAttackTimeClient = 0f;

    private Coroutine headbuttRoutine;
    private Coroutine clientLockRoutine;

    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (Time.time < nextAttackTimeServer) return;

        if (data is HeadbuttSkillData headbuttData && controller != null)
        {
            nextAttackTimeServer = Time.time + headbuttData.attackDelay + headbuttData.recoveryTime;

            currentHeadbuttData = headbuttData;
            if (headbuttRoutine != null) StopCoroutine(headbuttRoutine);
            headbuttRoutine = StartCoroutine(HeadbuttRoutine(headbuttData, controller));
        }
    }

    public override void ClientPlayVisual(SkillData data)
    {
        if (data is HeadbuttSkillData headbuttData)
        {
            if (Time.time < nextAttackTimeClient) return;
            nextAttackTimeClient = Time.time + headbuttData.attackDelay + headbuttData.recoveryTime;
        }

        base.ClientPlayVisual(data); 
        
        Animator anim = animator;
        if (anim == null) anim = GetComponentInParent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Headbut"))
            {
                anim.SetTrigger("Headbutt");
            }
        }
        
        if (data is HeadbuttSkillData hbData)
        {
            if (clientLockRoutine != null) StopCoroutine(clientLockRoutine);
            clientLockRoutine = StartCoroutine(ClientLockMovementRoutine(hbData));
        }
    }

    private IEnumerator ClientLockMovementRoutine(HeadbuttSkillData data)
    {
        PlayerController controller = GetComponentInParent<PlayerController>();
        if (controller == null) yield break;

        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();
        
        if (movement != null)
        {
            movement.isMovementLocked = true;
            yield return new WaitForSeconds(data.attackDelay + data.recoveryTime);
            movement.isMovementLocked = false;
        }
    }

    private IEnumerator HeadbuttRoutine(HeadbuttSkillData data, PlayerController controller)
    {
        PlayerMovement movement = controller.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = controller.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.isMovementLocked = true;

        float damageMultiplier = 1f;
        PlayerSkills skills = controller.GetComponentInChildren<PlayerSkills>();
        if (skills == null) skills = controller.GetComponentInParent<PlayerSkills>();
        if (skills != null) damageMultiplier = skills.damageMultiplier.Value;

        yield return new WaitForSeconds(data.attackDelay);

        Vector2 facingDir = Vector2.right;
        if (playerSprite != null && playerSprite.flipX) 
        {
            facingDir = Vector2.left;
        }

        Vector2 hitCenter = (Vector2)controller.transform.position + new Vector2(data.hitboxOffset.x * facingDir.x, data.hitboxOffset.y);

        LayerMask layer = enemyLayer != 0 ? enemyLayer : data.enemyLayer;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, data.hitRadius, layer);

        HashSet<Collider2D> damagedEnemies = new HashSet<Collider2D>();
        bool hasPlayedParticle = false; 

        foreach (var hit in hits)
        {
            if (damagedEnemies.Add(hit))
            {
                NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
                if (enemyEntity != null) 
                {
                    int finalDamage = Mathf.RoundToInt(data.damage * damageMultiplier);
                    enemyEntity.TakeDamage(finalDamage);
                    enemyEntity.ApplyKnockback(facingDir * data.knockbackForce, 0.2f);
                }

                if (impactParticle != null && !hasPlayedParticle)
                {
                    Vector3 impactPos = hit.ClosestPoint(hitCenter);
                    impactParticle.transform.position = impactPos;
                    float yRotation = facingDir.x < 0 ? 180f : 0f;
                    impactParticle.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
                    impactParticle.Play();

                    // RUNG CAMERA KHI HÚC TRÚNG
                    if (impulseSource != null)
                    {
                        impulseSource.GenerateImpulse();
                    }

                    hasPlayedParticle = true;
                }
                
                ClientPlayHitEffect(data, hit.transform.position);
            }
        }

        yield return new WaitForSeconds(data.recoveryTime);

        if (movement != null) movement.isMovementLocked = false;
        currentHeadbuttData = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentHeadbuttData != null)
        {
            Gizmos.color = Color.red;
            float facingX = 1f;
            if (playerSprite != null && playerSprite.flipX) facingX = -1f;
            
            Vector2 hitCenter = (Vector2)transform.position + new Vector2(currentHeadbuttData.hitboxOffset.x * facingX, currentHeadbuttData.hitboxOffset.y);
            Gizmos.DrawWireSphere(hitCenter, currentHeadbuttData.hitRadius);
        }
    }
}