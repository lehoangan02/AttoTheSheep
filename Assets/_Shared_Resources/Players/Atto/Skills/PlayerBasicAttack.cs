using UnityEngine;

// KẾ THỪA TỪ BaseSkillComponent
public class PlayerBasicAttack : BaseSkillComponent
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Animator animator; 
    
    // GHI ĐÈ HÀM SERVER ĐỂ TÍNH SÁT THƯƠNG
    public override void ServerExecute(SkillData data, NetworkEntity caster, PlayerController controller = null)
    {
        if (caster == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, caster.BaseAttackRange, enemyLayer);
        foreach (var hit in hits)
        {
            NetworkEntity enemyEntity = hit.GetComponent<NetworkEntity>();
            if (enemyEntity != null)
            {
                // Thay đổi: Có thể lấy data.damage cộng thêm caster.BaseAttackDamage
                enemyEntity.TakeDamage((int)caster.BaseAttackDamage + (int)data.damage);
            }
        }
    }

    // GHI ĐÈ HÀM CLIENT ĐỂ BẬT ANIMATION
    public override void ClientPlayVisual(SkillData data)
    {
        if (animator != null) animator.SetTrigger("BasicAttack");
    }
}