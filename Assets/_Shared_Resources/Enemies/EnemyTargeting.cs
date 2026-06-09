using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    [SerializeField] private float scanRadius = 30f;
    [SerializeField] private LayerMask targetLayers = ~0;

    public NetworkEntity FindNearestTarget(Vector2 origin)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, scanRadius, targetLayers);
        NetworkEntity nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            NetworkEntity candidate = hit.GetComponentInParent<NetworkEntity>();
            if (!IsValidTarget(candidate)) continue;

            float distanceSqr = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr >= nearestDistanceSqr) continue;

            nearest = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest;
    }

    private static bool IsValidTarget(NetworkEntity candidate)
    {
        if (candidate == null) return false;
        // if (candidate is EnemyEntity) return false;
        // if (candidate.currentHealth.Value <= 0) return false;

        // string tag = candidate.gameObject.tag;

        // return candidate.GetComponent<PlayerEntity>() != null
        //     || candidate.GetComponentInChildren<PlayerController>() != null
        //     || candidate.GetComponent<LambAI>() != null
        //     || tag == "Player"
        //     || tag == "Lamb"
        //     || tag == "Sheep";

        return candidate.CompareTag("Player");
    }
}
