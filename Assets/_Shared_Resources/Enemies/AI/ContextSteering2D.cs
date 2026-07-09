using UnityEngine;

/// <summary>
/// Simple Reynolds-style steering for 2D enemies.
///
/// Composes three vectors each FixedUpdate:
///   - Seek (or full orbit/strafe when within range and attack is on cooldown)
///   - Wall avoidance (body-radius-aware CircleCast whiskers ahead)
///   - Ally separation (OverlapCircle of nearby enemies)
///
/// The combined direction is normalized and smoothed across frames.
///
/// Designed for top-down RPG combat in open spaces / scattered obstacle layouts.
/// It is NOT a pathfinder: long fully-blocking walls with no opening may cause
/// the enemy to slide along them, but it will round finite walls and pillars.
///
/// Driven from EnemyBrain.MoveChaseTarget(). Public API unchanged:
///   SteeringOrigin
///   ComputeDirection(toTargetDir, distanceToTarget, allowStrafe)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ContextSteering2D : MonoBehaviour
{
    [Header("Layers")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask allyMask;

    [Header("Avoidance")]
    [SerializeField] private float sensorLength = 1.2f;
    [SerializeField] private float avoidWeight = 2f;

    [Header("Ally Separation")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationWeight = 1f;

    [Header("Strafe")]
    [SerializeField] private float strafeRange = 2f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    [SerializeField] private float directionSmoothing = 0.4f;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    Vector2 _debugSeek;
    Vector2 _debugAvoid;
    Vector2 _debugSep;
    Vector2 _debugOutput;
#endif

    public Vector2 SteeringOrigin => (Vector2)transform.position + steeringOriginOffset;

    Vector2 steeringOriginOffset;
    float bodyRadius;
    Collider2D selfCollider;

    Vector2 lastOutputDir;
    int strafeSign = 1;
    float lastStrafeFlipTime;

    const float WHISKER_ANGLE = 35f;
    const float STRAFE_STICK_TIME = 0.8f;
    const float ARRIVAL_PAUSE = 1f;
    const int MAX_ALLY_HITS = 32;
    static readonly Collider2D[] s_allyHits = new Collider2D[MAX_ALLY_HITS];

    void Awake()
    {
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Building", "Terrain");
        if (allyMask.value == 0)
            allyMask = LayerMask.GetMask("Enemy");

        selfCollider = GetComponent<Collider2D>();
        steeringOriginOffset = selfCollider != null ? selfCollider.offset : Vector2.zero;
        bodyRadius = GetBodyRadius();
    }

    float GetBodyRadius()
    {
        if (selfCollider is CircleCollider2D c) return c.radius;
        if (selfCollider is CapsuleCollider2D cap) return cap.size.x * 0.5f;
        if (selfCollider is BoxCollider2D box) return Mathf.Min(box.size.x, box.size.y) * 0.5f;
        return 0.3f;
    }

    /// <summary>
    /// Returns the desired normalized movement direction for this FixedUpdate.
    /// </summary>
    public Vector2 ComputeDirection(Vector2 toTargetDir, float distanceToTarget, bool allowStrafe = true)
    {
        Vector2 origin = SteeringOrigin;

        // 1. Seek toward target, or orbit/strafe when in range.
        Vector2 seek;
        if (allowStrafe && distanceToTarget <= strafeRange && distanceToTarget > 0.01f)
        {
            PickStrafeSide(toTargetDir, origin);
            seek = Perpendicular(toTargetDir, strafeSign);
        }
        else
        {
            float arrival = distanceToTarget < ARRIVAL_PAUSE
                ? distanceToTarget / ARRIVAL_PAUSE
                : 1f;
            seek = toTargetDir * arrival;
        }

        // 2. Avoid walls and obstacles.
        Vector2 avoid = ComputeAvoidance(origin, seek);

        // 3. Push away from overlapping allies.
        Vector2 separation = ComputeSeparation(origin);

        // 4. Combine and normalize.
        Vector2 desired = seek + avoid * avoidWeight + separation * separationWeight;

        if (desired.sqrMagnitude < 0.0001f)
            desired = lastOutputDir.sqrMagnitude > 0.0001f ? lastOutputDir : Vector2.up;

        Vector2 output = desired.normalized;

        // 5. Smooth across frames.
        output = SmoothDirection(output, lastOutputDir, directionSmoothing, 0.5f);
        lastOutputDir = output;

#if UNITY_EDITOR
        _debugSeek = seek;
        _debugAvoid = avoid;
        _debugSep = separation;
        _debugOutput = output;
#endif

        return output;
    }

    Vector2 ComputeAvoidance(Vector2 origin, Vector2 referenceDir)
    {
        Vector2 avoid = Vector2.zero;
        if (referenceDir.sqrMagnitude < 0.0001f)
            referenceDir = Vector2.up;

        Probe(origin, referenceDir, sensorLength, ref avoid);
        Probe(origin, Rotate(referenceDir, WHISKER_ANGLE), sensorLength, ref avoid);
        Probe(origin, Rotate(referenceDir, -WHISKER_ANGLE), sensorLength, ref avoid);

        return avoid;
    }

    void Probe(Vector2 origin, Vector2 dir, float length, ref Vector2 accumulator)
    {
        RaycastHit2D hit = Physics2D.CircleCast(origin, bodyRadius, dir, length, obstacleMask);
        if (!hit.collider) return;

        float strength = 1f - (hit.distance / length);
        if (strength <= 0f) return;

        // Surface normal pushes us off/around the obstacle.
        accumulator += hit.normal * strength;
    }

    Vector2 ComputeSeparation(Vector2 origin)
    {
        Vector2 separation = Vector2.zero;
        var filter = new ContactFilter2D { layerMask = allyMask, useLayerMask = true };
        int hitCount = Physics2D.OverlapCircle(origin, separationRadius, filter, s_allyHits);
        if (hitCount <= 0) return separation;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = s_allyHits[i];
            if (col == null || col == selfCollider) continue;

            Vector2 toAlly = (Vector2)col.transform.position - origin;
            float dist = toAlly.magnitude;
            if (dist < 0.001f || dist >= separationRadius) continue;

            float strength = 1f - (dist / separationRadius);
            separation += (-toAlly / dist) * strength;
        }

        return separation;
    }

    void PickStrafeSide(Vector2 toTargetDir, Vector2 origin)
    {
        Vector2 perpR = Perpendicular(toTargetDir, 1);
        Vector2 perpL = Perpendicular(toTargetDir, -1);

        float clearR = CastDistance(origin, perpR);
        float clearL = CastDistance(origin, perpL);

        float timeSinceFlip = Time.time - lastStrafeFlipTime;
        if (timeSinceFlip < STRAFE_STICK_TIME) return;

        if (strafeSign > 0 && clearR < clearL)
        {
            strafeSign = -1;
            lastStrafeFlipTime = Time.time;
        }
        else if (strafeSign < 0 && clearL < clearR)
        {
            strafeSign = 1;
            lastStrafeFlipTime = Time.time;
        }
    }

    float CastDistance(Vector2 origin, Vector2 dir)
    {
        RaycastHit2D hit = Physics2D.CircleCast(origin, bodyRadius, dir, sensorLength, obstacleMask);
        return hit.collider ? hit.distance : sensorLength;
    }

    static Vector2 SmoothDirection(Vector2 current, Vector2 previous, float factor, float opposeThreshold)
    {
        if (previous.sqrMagnitude < 0.0001f) return current;
        if (current.sqrMagnitude < 0.0001f) return Vector2.zero;
        if (Vector2.Dot(current, previous) < -opposeThreshold) return current;
        return Vector2.Lerp(current, previous, factor).normalized;
    }

    static Vector2 Perpendicular(Vector2 v, int sign)
    {
        return sign >= 0 ? new Vector2(v.y, -v.x) : new Vector2(-v.y, v.x);
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        if (bodyRadius <= 0f) return;

        Vector3 pos = SteeringOrigin;

        Gizmos.color = new Color(0f, 1f, 0f, 0.75f);
        Gizmos.DrawRay(pos, (Vector3)_debugSeek.normalized * sensorLength);

        Gizmos.color = new Color(1f, 0f, 0f, 0.75f);
        Gizmos.DrawRay(pos, (Vector3)_debugAvoid.normalized * sensorLength);

        Gizmos.color = new Color(1f, 0f, 1f, 0.75f);
        Gizmos.DrawRay(pos, (Vector3)_debugSep.normalized * sensorLength);

        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        Gizmos.DrawRay(pos, (Vector3)_debugOutput * sensorLength);

        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(pos, separationRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(pos, strafeRange);
    }
#endif
}
