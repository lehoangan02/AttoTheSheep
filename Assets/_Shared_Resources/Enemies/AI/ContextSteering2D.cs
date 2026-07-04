using UnityEngine;

/// <summary>
/// Context-Based Steering for 2D enemy AI.
/// 8-way compass: Interest map (dot product against target direction),
/// Danger map (8 raycasts against obstacle layer + ally overlap sensor),
/// Final = max(0, Interest - Danger), blend top angular neighbors.
///
/// Driven from EnemyBrain.MoveChaseTarget() in FixedUpdate. Server-side only
/// (per-enemy cost is 8 raycasts + 1 overlap per tick, but scratch buffers
/// are static, so zero per-frame allocations).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ContextSteering2D : MonoBehaviour
{
    [Header("Compass")]
    [SerializeField] private int rayCount = 8;
    [SerializeField] private float sensorLength = 1.2f;

    [Header("Layer Masks (defaults set in Awake)")]
    [SerializeField] private LayerMask obstacleMask; // default = Building|Terrain
    [SerializeField] private LayerMask allyMask;     // default = Enemy

    [Header("Shaping - Distance")]
    [SerializeField] private float approachRange = 5f;
    [SerializeField] private float approachRangePause = 1.5f;

    [Header("Shaping - Strafe")]
    [SerializeField] private bool enableStrafe = true;
    [SerializeField] private float strafeRange = 2f;

    [Header("Separation")]
    [SerializeField] private float separationBiasAngle = 35f; // +clockwise, "pass right"
    [SerializeField] private float allyScanRadius = 2.5f;
    [SerializeField] private float closeRepulsionRadius = 0.8f;
    [SerializeField] private float closeRepulsionStrength = 2f;

    [Header("Synthesis")]
    [SerializeField] private float blendAngleThreshold = 45f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    // Cached runtime fields (private, NOT SerializeField):
    Vector2[] dirs8;
    float[] interest;
    float[] danger;
    float[] finalScores;     // max(0, interest - danger) for blending and gizmos
    int strafeSign = 1;
    float lastStrafeDot;
    bool inStrafeMode;
    Vector2 lastBlendedDir;  // for gizmos
    Vector2 lastStrafeDir;   // for gizmos
    Rigidbody2D rb;
    Collider2D selfCollider;
    Vector2 steeringOriginOffset;
    public Vector2 SteeringOrigin => (Vector2)transform.position + steeringOriginOffset;

    // Static scratch buffers (shared, safe: single-threaded FixedUpdate).
    // Safe ONLY because Unity's FixedUpdate is single-threaded.
    // If Burst/Jobs are added later, move to instance buffers.
    // Sized for max 32 rays (rayCount is configurable)
    static readonly RaycastHit2D[] s_wallHits = new RaycastHit2D[32];
    static readonly Collider2D[] s_allyHits = new Collider2D[16];

    void Awake()
    {
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Building", "Terrain");
        if (allyMask.value == 0)
            allyMask = LayerMask.GetMask("Enemy");

        rb = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();
        steeringOriginOffset = selfCollider != null ? selfCollider.offset : Vector2.zero;

        // Build the compass directions (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
        dirs8 = new Vector2[rayCount];
        interest = new float[rayCount];
        danger = new float[rayCount];
        finalScores = new float[rayCount];
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * 360f / rayCount * Mathf.Deg2Rad;
            dirs8[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    /// <summary>
    /// Compute the context-steered movement direction.
    /// Called from EnemyBrain.MoveChaseTarget(). Runs server-side only.
    /// </summary>
    /// <param name="toTargetDir">Normalized direction from self to target.</param>
    /// <param name="distanceToTarget">Current distance to target.</param>
    /// <returns>Normalized blended direction, or Vector2.zero if no viable path.</returns>
    public Vector2 ComputeDirection(Vector2 toTargetDir, float distanceToTarget, bool allowStrafe = true)
    {
        // 1. Compute base interest (dot product of each compass dir vs target direction)
        SteeringMath.ComputeInterest(toTargetDir, dirs8, interest);

        // 2. Apply distance scaling: within approachRange, reduce toward-target interest
        //    Skip when attack is ready (allowStrafe=false) so enemy beelines in
        if (allowStrafe)
            SteeringMath.ApplyDistanceScaling(interest, distanceToTarget, approachRange, approachRangePause);

        // 3. If strafe enabled and within strafeRange, replace interest with perpendicular direction
        if (enableStrafe && allowStrafe && distanceToTarget <= strafeRange)
        {
            SteeringMath.ApplyStrafe(interest, dirs8, toTargetDir, transform, ref strafeSign, ref inStrafeMode, strafeRange);
            lastStrafeDir = SteeringMath.Perpendicular(toTargetDir, strafeSign);
        }
        else
        {
            if (inStrafeMode && distanceToTarget > strafeRange * 1.1f)
                inStrafeMode = false; // exit with hysteresis
            lastStrafeDir = Vector2.zero;
        }

        // 4. Compute wall danger (8 raycasts against Building|Terrain)
        SteeringMath.ComputeDangerRay(SteeringOrigin, dirs8, sensorLength, obstacleMask, s_wallHits, danger);

        // 5. Add ally separation danger (OverlapCircle for nearby Enemy-layer entities, rotated by bias angle)
        SteeringMath.AddAllyDanger(SteeringOrigin, allyScanRadius, allyMask, selfCollider, s_allyHits, dirs8, separationBiasAngle, danger, closeRepulsionRadius, closeRepulsionStrength);

        // 6. Final = max(0, Interest - Danger) per slot
        for (int i = 0; i < interest.Length; i++)
            finalScores[i] = Mathf.Max(0f, interest[i] - danger[i]);

        // 7. Blend top angular neighbors, store for gizmos
        lastBlendedDir = SteeringMath.Blend(finalScores, dirs8, blendAngleThreshold);
        return lastBlendedDir;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (dirs8 == null) return;
        Vector3 pos = SteeringOrigin;

        // Draw range circles
        Gizmos.color = new Color(1, 1, 1, 0.15f);
        Gizmos.DrawWireSphere(pos, approachRange);
        Gizmos.color = new Color(0, 1, 1, 0.15f);
        Gizmos.DrawWireSphere(pos, strafeRange);

        if (interest == null || danger == null || finalScores == null) return;

        for (int i = 0; i < dirs8.Length; i++)
        {
            Vector3 dir = (Vector3)dirs8[i];

            // Green ray: interest
            if (interest[i] > 0.01f)
            {
                Gizmos.color = new Color(0, 1, 0, 0.6f);
                Gizmos.DrawRay(pos, dir * sensorLength * interest[i]);
            }

            // Red ray: danger (offset slightly so red/green don't perfectly overlap)
            if (danger[i] > 0.01f)
            {
                Gizmos.color = new Color(1, 0, 0, 0.6f);
                Vector3 offset = pos + new Vector3(dir.y, -dir.x, 0) * 0.05f;
                Gizmos.DrawRay(offset, dir * sensorLength * danger[i]);
            }

            // White sphere at tip: final score magnitude
            float score = finalScores[i];
            if (score > 0.01f)
            {
                Gizmos.color = Color.white;
                Vector3 tip = pos + dir * sensorLength * score;
                Gizmos.DrawSphere(tip, 0.03f);
            }
        }

        // Yellow arrow: final blended direction
        if (lastBlendedDir != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(pos, (Vector3)lastBlendedDir * sensorLength);
            Vector3 end = pos + (Vector3)lastBlendedDir * sensorLength;
            Vector3 right = Quaternion.Euler(0, 0, 160) * (Vector3)lastBlendedDir * 0.2f;
            Vector3 left  = Quaternion.Euler(0, 0, -160) * (Vector3)lastBlendedDir * 0.2f;
            Gizmos.DrawRay(end, right);
            Gizmos.DrawRay(end, left);
        }

        // Cyan ray: strafe perpendicular basis when in strafe mode
        if (inStrafeMode && lastStrafeDir != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pos, (Vector3)lastStrafeDir * sensorLength * 0.5f);
        }
    }
#endif
}

/// <summary>
/// Pure math helpers for context steering. No Time, no Physics2D calls, no
/// static UnityEngine state — inputs are all values or scratch arrays, so
/// these methods are deterministic and unit-testable.
/// </summary>
public static class SteeringMath
{
    /// <summary>Compute interest per compass slot: dot product of each dir vs target direction, clamped [0,1].</summary>
    public static void ComputeInterest(Vector2 toTargetDir, Vector2[] dirs, float[] outInterest)
    {
        for (int i = 0; i < dirs.Length; i++)
            outInterest[i] = Mathf.Clamp01(Vector2.Dot(dirs[i], toTargetDir));
    }

    /// <summary>Scale interest toward 0 as distance goes from fullAt → zeroAt. Linear lerp.</summary>
    public static void ApplyDistanceScaling(float[] interest, float dist, float fullAt, float zeroAt)
    {
        if (dist >= fullAt) return;
        float t = Mathf.Clamp01((dist - zeroAt) / (fullAt - zeroAt));
        for (int i = 0; i < interest.Length; i++)
            interest[i] *= t;
    }

    /// <summary>Blend strafe perpendicular into interest. Called when within strafeRange.
    /// Sign selection: on entry, picks the sign that keeps current facing (dot with self.right). Cached with hysteresis.</summary>
    public static void ApplyStrafe(float[] interest, Vector2[] dirs, Vector2 toTargetDir, Transform self, ref int strafeSign, ref bool inStrafeMode, float strafeRange)
    {
        if (!inStrafeMode)
        {
            inStrafeMode = true;
            Vector2 perpRight = Perpendicular(toTargetDir, 1);
            float dot = Vector2.Dot(self.right, perpRight);
            strafeSign = dot >= 0f ? 1 : -1;
        }
        Vector2 strafeDir = Perpendicular(toTargetDir, strafeSign);
        ComputeInterest(strafeDir, dirs, interest);
        // NOTE: distance scaling still applies from the caller (already applied before ApplyStrafe)
    }

    /// <summary>8 raycasts. danger[i] = 1 - hit.distance/sensorLength if hit, else 0.</summary>
    public static void ComputeDangerRay(Vector2 origin, Vector2[] dirs, float sensorLength, LayerMask obstacleMask, RaycastHit2D[] wallHits, float[] outDanger)
    {
        for (int i = 0; i < dirs.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, dirs[i], sensorLength, obstacleMask);
            wallHits[i] = hit;
            outDanger[i] = hit.collider ? 1f - (hit.distance / sensorLength) : 0f;
        }
    }

    /// <summary>OverlapCircle for nearby Enemy-layer colliders. For each ally, weight (1-dist/radius)
    /// into the NEAREST compass slot, then rotate the slot index by biasAngle (clockwise +).</summary>
    public static void AddAllyDanger(Vector2 origin, float scanRadius, LayerMask allyMask, Collider2D self, Collider2D[] allyHits, Vector2[] dirs, float biasAngle, float[] outDanger, float closeRepulsionRadius = 0f, float closeRepulsionStrength = 1f)
    {
        int count = Physics2D.OverlapCircleNonAlloc(origin, scanRadius, allyHits, allyMask);
        if (count <= 0) return;
        float anglePerSlot = 360f / dirs.Length;
        int biasSlots = Mathf.RoundToInt(biasAngle / anglePerSlot); // convert degrees to nearest slot offset

        for (int j = 0; j < count; j++)
        {
            Collider2D col = allyHits[j];
            if (col == null || col == self) continue;
            Vector2 toAlly = (Vector2)(col.bounds.center - (Vector3)origin);
            float dist = toAlly.magnitude;
            if (dist < 0.001f) continue;
            float weight = 1f - (dist / scanRadius);
            if (weight <= 0f) continue;

            // Extra repulsion for very close allies
            if (dist < closeRepulsionRadius)
                weight = Mathf.Max(weight, closeRepulsionStrength * (1f - dist / closeRepulsionRadius));

            // Find nearest compass slot (unbiased)
            float goalAngle = Mathf.Atan2(toAlly.y, toAlly.x) * Mathf.Rad2Deg;
            int nearest = Mathf.RoundToInt(goalAngle / anglePerSlot);
            if (nearest < 0) nearest += dirs.Length;

            // Apply bias: shift slot by biasSlots (clockwise +), wrap
            int biasedSlot = (nearest + biasSlots) % dirs.Length;
            if (biasedSlot < 0) biasedSlot += dirs.Length;

            outDanger[biasedSlot] = Mathf.Min(1f, outDanger[biasedSlot] + weight);
        }
    }

    /// <summary>Blend best slot with its neighbors. Linear weight by angular proximity.
    /// Returns zero if all final ≤ 0.</summary>
    public static Vector2 Blend(float[] final, Vector2[] dirs, float neighborAngleThreshold)
    {
        int bestIdx = 0;
        float bestVal = final[0];
        for (int i = 1; i < final.Length; i++)
        {
            if (final[i] > bestVal) { bestVal = final[i]; bestIdx = i; }
        }
        if (bestVal <= 0f) return Vector2.zero;

        Vector2 blended = dirs[bestIdx] * bestVal;
        float totalWeight = bestVal;
        float anglePerSlot = 360f / dirs.Length;

        for (int i = 0; i < final.Length; i++)
        {
            if (i == bestIdx) continue;
            if (final[i] <= 0f) continue;
            int slotDiff = Mathf.Abs(i - bestIdx);
            if (slotDiff > final.Length / 2) slotDiff = final.Length - slotDiff;
            float angleDiff = slotDiff * anglePerSlot;
            if (angleDiff > neighborAngleThreshold) continue;

            float weight = final[i] * (1f - (angleDiff / neighborAngleThreshold));
            blended += dirs[i] * weight;
            totalWeight += weight;
        }

        return totalWeight > 0.0001f ? blended.normalized : Vector2.zero;
    }

    /// <summary>Get perpendicular vector: sign≥0 → right, sign&lt;0 → left.</summary>
    public static Vector2 Perpendicular(Vector2 v, int sign)
    {
        return sign >= 0 ? new Vector2(v.y, -v.x) : new Vector2(-v.y, v.x);
    }
}
