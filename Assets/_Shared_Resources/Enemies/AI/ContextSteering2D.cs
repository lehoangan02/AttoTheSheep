using UnityEngine;

/// <summary>
/// Context-Based Steering for 2D enemy AI (server-side FixedUpdate).
///
/// 16-way compass: Interest map (dot against target), Danger map
/// (CircleCast per direction against obstacle layer + OverlapCircle
/// for ally separation), Final = max(0, Interest - Danger), blend
/// top angular neighbors, then smooth across frames.
///
/// Wall avoidance uses CircleCast (body-radius-aware) with danger
/// propagated to angular neighbors, so walls become "thick" in
/// danger space — wall-following emerges and corners stop sticking.
///
/// Ally separation writes continuous anti-direction danger with the
/// same neighbor propagation for smooth separation without clumping.
///
/// Strafe is a blended bias (not a full interest replacement) that
/// picks the more-open perpendicular side dynamically with hysteresis,
/// and still passes through the full danger mask.
///
/// Fallback when fully blocked: picks the least-danger direction
/// instead of freezing, so the enemy slides along walls.
///
/// Driven from EnemyBrain.MoveChaseTarget() every FixedUpdate.
/// Public API unchanged: ComputeDirection(toTargetDir, dist, canStrafe).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ContextSteering2D : MonoBehaviour
{
    [Header("Compass")]
    [SerializeField] private int rayCount = 16;
    [SerializeField] private float baseSensorLength = 1.2f;
    [SerializeField] private float speedSensorFactor = 0.25f;
    [SerializeField] private float maxSensorLength = 3.0f;

    [Header("Danger Propagation")]
    [SerializeField] private float[] dangerFalloff = { 1f, 0.5f, 0.2f };

    [Header("Layer Masks (defaults set in Awake)")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask allyMask;

    [Header("Shaping - Distance")]
    [SerializeField] private float approachRange = 5f;
    [SerializeField] private float approachRangePause = 1.5f;

    [Header("Shaping - Strafe")]
    [SerializeField] private bool enableStrafe = true;
    [SerializeField] private float strafeRange = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float strafeWeight = 0.7f;
    [SerializeField] private float strafeFlipMargin = 0.3f;
    [SerializeField] private float strafeStickTime = 1.0f;

    [Header("Separation")]
    [SerializeField] private float allyScanRadius = 2.5f;
    [SerializeField] private float closeRepulsionRadius = 0.8f;
    [SerializeField] private float closeRepulsionStrength = 1f;

    [Header("Synthesis")]
    [SerializeField] private float blendAngleThreshold = 45f;
    [Range(0f, 1f)]
    [SerializeField] private float directionSmoothing = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    Vector2[] dirs;
    float[] interest;
    float[] strafeInterest;
    float[] danger;
    float[] finalScores;

    int strafeSign = 1;
    float lastStrafeFlipTime;

    Vector2 lastOutputDir;
    Vector2 lastBlendedDir;
    Vector2 lastStrafeDir;
    float lastSensorLength;

    Collider2D selfCollider;
    float bodyRadius;
    Vector2 steeringOriginOffset;
    Rigidbody2D rb;

    public Vector2 SteeringOrigin => (Vector2)transform.position + steeringOriginOffset;

    static readonly Collider2D[] s_allyHits = new Collider2D[32];

    void Awake()
    {
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Building", "Terrain");
        if (allyMask.value == 0)
            allyMask = LayerMask.GetMask("Enemy");

        rb = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();
        steeringOriginOffset = selfCollider != null ? selfCollider.offset : Vector2.zero;

        bodyRadius = GetBodyRadius();

        dirs = SteeringMath.BuildDirections(rayCount);
        interest = new float[rayCount];
        strafeInterest = new float[rayCount];
        danger = new float[rayCount];
        finalScores = new float[rayCount];
    }

    float GetBodyRadius()
    {
        if (selfCollider is CircleCollider2D c) return c.radius;
        if (selfCollider is CapsuleCollider2D cap) return cap.size.x * 0.5f;
        if (selfCollider is BoxCollider2D box) return Mathf.Min(box.size.x, box.size.y) * 0.5f;
        return 0.3f;
    }

    public Vector2 ComputeDirection(Vector2 toTargetDir, float distanceToTarget, bool allowStrafe = true)
    {
        Vector2 origin = SteeringOrigin;

        // 1. Seek interest
        SteeringMath.ComputeInterest(toTargetDir, dirs, interest);

        // 2. Distance scaling (arrival throttle)
        if (allowStrafe)
            SteeringMath.ApplyDistanceScaling(interest, distanceToTarget, approachRange, approachRangePause);

        // 3. Speed-scaled sensor length
        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        lastSensorLength = Mathf.Clamp(baseSensorLength + speed * speedSensorFactor, baseSensorLength, maxSensorLength);

        // 4. Zero danger, then compute obstacle + ally danger
        System.Array.Clear(danger, 0, danger.Length);
        ComputeObstacleDanger(origin);
        ComputeAllyDanger(origin);

        // 5. Strafe blend
        if (enableStrafe && allowStrafe && distanceToTarget <= strafeRange && distanceToTarget > 0.01f)
        {
            PickStrafeSide(toTargetDir);
            Vector2 strafeDir = SteeringMath.Perpendicular(toTargetDir, strafeSign);
            lastStrafeDir = strafeDir;

            SteeringMath.ComputeInterest(strafeDir, dirs, strafeInterest);

            float seekW = 1f - strafeWeight;
            for (int i = 0; i < rayCount; i++)
                interest[i] = seekW * interest[i] + strafeWeight * strafeInterest[i];
        }
        else
        {
            lastStrafeDir = Vector2.zero;
        }

        // 6. Final = max(0, Interest - Danger)
        for (int i = 0; i < rayCount; i++)
            finalScores[i] = Mathf.Max(0f, interest[i] - danger[i]);

        // 7. Blend top angular neighbors (or fallback if all zero)
        Vector2 blended = SteeringMath.Blend(finalScores, dirs, blendAngleThreshold);
        if (blended == Vector2.zero)
            blended = SteeringMath.LeastDangerDirection(danger, dirs);

        lastBlendedDir = blended;

        // 8. Smooth across frames (skip on sharp turns)
        blended = SteeringMath.SmoothDirection(blended, lastOutputDir, directionSmoothing, 0.5f);
        lastOutputDir = blended;

        return blended;
    }

    void ComputeObstacleDanger(Vector2 origin)
    {
        int count = rayCount;
        float len = lastSensorLength;
        LayerMask mask = obstacleMask;
        float radius = bodyRadius;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origin, radius, dirs[i], len, mask);
            if (!hit.collider) continue;

            float weight = 1f - (hit.distance / len);
            if (weight <= 0f) continue;

            SteeringMath.ApplyDangerFalloff(danger, i, weight, dangerFalloff, count);
        }
    }

    void ComputeAllyDanger(Vector2 origin)
    {
        var filter = new ContactFilter2D { layerMask = allyMask };
        int hitCount = Physics2D.OverlapCircle(origin, allyScanRadius, filter, s_allyHits);
        if (hitCount <= 0) return;

        int dirCount = rayCount;
        float scanInv = 1f / allyScanRadius;
        float closeInv = closeRepulsionRadius > 0.001f ? 1f / closeRepulsionRadius : 0f;

        for (int j = 0; j < hitCount; j++)
        {
            Collider2D col = s_allyHits[j];
            if (col == null || col == selfCollider) continue;

            Vector2 toAlly = (Vector2)col.transform.position - origin;
            float dist = toAlly.magnitude;
            if (dist < 0.001f) continue;

            float weight = 1f - dist * scanInv;
            if (weight <= 0f) continue;

            if (closeRepulsionRadius > 0.001f && dist < closeRepulsionRadius)
            {
                float closeWeight = closeRepulsionStrength * (1f - dist * closeInv);
                if (closeWeight > weight) weight = closeWeight;
            }

            int slot = SteeringMath.GetNearestSlot(toAlly / dist, dirCount);
            SteeringMath.ApplyDangerFalloff(danger, slot, weight, dangerFalloff, dirCount);
        }
    }

    void PickStrafeSide(Vector2 toTargetDir)
    {
        Vector2 perpR = SteeringMath.Perpendicular(toTargetDir, 1);
        Vector2 perpL = SteeringMath.Perpendicular(toTargetDir, -1);

        int dirCount = rayCount;
        int slotR = SteeringMath.GetNearestSlot(perpR, dirCount);
        int slotL = SteeringMath.GetNearestSlot(perpL, dirCount);

        float dangerR = danger[slotR];
        float dangerL = danger[slotL];

        float timeSinceFlip = Time.time - lastStrafeFlipTime;
        float margin = strafeFlipMargin;

        if (strafeSign == 1)
        {
            if (dangerR > dangerL + margin && timeSinceFlip >= strafeStickTime)
            {
                strafeSign = -1;
                lastStrafeFlipTime = Time.time;
            }
        }
        else
        {
            if (dangerL > dangerR + margin && timeSinceFlip >= strafeStickTime)
            {
                strafeSign = 1;
                lastStrafeFlipTime = Time.time;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (dirs == null || interest == null || danger == null || finalScores == null) return;

        Vector3 pos = SteeringOrigin;
        float gizmoLen = lastSensorLength > 0.01f ? lastSensorLength : baseSensorLength;

        // Range circles
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, approachRange);
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, strafeRange);

        // Per-slot rays — green = interest, red = danger, white sphere = final
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 dir = (Vector3)dirs[i];

            if (interest[i] > 0.01f)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
                Gizmos.DrawRay(pos, dir * gizmoLen * interest[i]);
            }

            if (danger[i] > 0.01f)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                Vector3 offset = pos + new Vector3(dir.y, -dir.x, 0f) * 0.05f;
                Gizmos.DrawRay(offset, dir * gizmoLen * danger[i]);
            }

            if (finalScores[i] > 0.01f)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(pos + dir * gizmoLen * finalScores[i], 0.03f);
            }
        }

        // Yellow arrow — final blended direction
        if (lastBlendedDir != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Vector3 end = pos + (Vector3)lastBlendedDir * gizmoLen;
            Gizmos.DrawRay(pos, (Vector3)lastBlendedDir * gizmoLen);
            Vector3 r = Quaternion.Euler(0f, 0f, 160f) * (Vector3)lastBlendedDir * 0.2f;
            Vector3 l = Quaternion.Euler(0f, 0f, -160f) * (Vector3)lastBlendedDir * 0.2f;
            Gizmos.DrawRay(end, r);
            Gizmos.DrawRay(end, l);
        }

        // Red arrow — smoothed output direction (actual movement)
        if (lastOutputDir != Vector2.zero)
        {
            Gizmos.color = new Color(1f, 1f, 0.5f, 0.8f);
            Vector3 offset = pos + new Vector3(0f, 0.1f, 0f);
            Gizmos.DrawRay(offset, (Vector3)lastOutputDir * gizmoLen * 0.5f);
        }

        // Cyan ray — strafe perpendicular direction
        if (lastStrafeDir != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, (Vector3)lastStrafeDir * gizmoLen * 0.5f);
        }
    }
#endif
}
