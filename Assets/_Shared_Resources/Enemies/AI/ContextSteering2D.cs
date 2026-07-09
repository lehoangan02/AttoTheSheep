using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Bug2-based steering for 2D enemies.
///
/// Reaches the player with a hybrid Bug2 boundary-following algorithm layered on top of a
/// short-range reactive sensor. Long walls and concave pockets that defeated the previous
/// Reynolds-style whiskers are now navigable; close-range strafing is preserved.
///
/// Public API (unchanged):
///   SteeringOrigin
///   ComputeDirection(toTargetDir, distanceToTarget, allowStrafe)
///
/// Driven from EnemyBrain.MoveChaseTarget().
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ContextSteering2D : MonoBehaviour
{
    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Strafe")]
    [SerializeField] private float strafeRange = 2f;

    [Header("Bug2 Sensors")]
    [Tooltip("Forward reach used to detect when the straight path to the target is blocked.")]
    [SerializeField] private float forwardSensorLength = 1.2f;

    [Tooltip("Side reach used while tracing an obstacle boundary.")]
    [SerializeField] private float sideSensorLength = 0.8f;

    [Tooltip("Desired distance from the steering origin to the wall surface while wall-following. Will be clamped to at least the agent's body radius.")]
    [SerializeField] private float sideTargetOffset = 0.3f;

    [Tooltip("Gain that converts wall-distance error into a lateral correction.")]
    [SerializeField] private float sideSeekCoeff = 1.5f;

    [Tooltip("Degrees per second the agent rotates toward the committed wall side when a corner is met.")]
    [SerializeField] private float cornerTurnRate = 90f;

    [Header("Leave-Wall Conditions")]
    [Tooltip("Soft cap on how long the agent will stay in wall-follow before forcing a retry.")]
    [SerializeField] private float maxWallFollowTime = 6f;

    [Header("Smoothing")]
    [Tooltip("Distance within which arrival slowdown is applied in Seek state.")]
    [SerializeField] private float arrivalPause = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float directionSmoothing = 0.4f;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
#endif

    public Vector2 SteeringOrigin => (Vector2)transform.position + steeringOriginOffset;

    enum BugState { Seek, WallFollow }

    // Cached self-data
    Vector2 steeringOriginOffset;
    float bodyRadius;
    Collider2D selfCollider;

    // Output smoothing
    Vector2 lastOutputDir;

    // Strafe state
    int strafeSign = 1;
    float lastStrafeFlipTime;

    // Bug2 state
    BugState state = BugState.Seek;
    int wallSide = 1;                // +1 = right-hand wall follow, -1 = left-hand
    Vector2 hitPoint;                // Point where we first touched the obstacle
    float wallFollowTimer;

    const float STRAFE_STICK_TIME = 0.8f;

    void Awake()
    {
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Building", "Terrain");

        selfCollider = GetComponent<Collider2D>();
        steeringOriginOffset = selfCollider != null ? selfCollider.offset : Vector2.zero;
        bodyRadius = GetBodyRadius();

        // Scale sensor geometry with body size so large enemies don't outgrow their whiskers
        // and so the wall standoff is always achievable.
        forwardSensorLength = Mathf.Max(forwardSensorLength, bodyRadius * 2f);
        sideTargetOffset = Mathf.Max(sideTargetOffset, bodyRadius + 0.05f);
        sideSensorLength = Mathf.Max(sideSensorLength, sideTargetOffset + 0.25f);

        lastOutputDir = Vector2.up;
    }

    float GetBodyRadius()
    {
        if (selfCollider is CircleCollider2D c) return c.radius;
        if (selfCollider is CapsuleCollider2D cap) return cap.size.x * 0.5f;
        if (selfCollider is BoxCollider2D box) return Mathf.Min(box.size.x, box.size.y) * 0.5f;
        return 0.3f;
    }

    void Reset()
    {
        obstacleMask = LayerMask.GetMask("Building", "Terrain");
    }

    /// <summary>
    /// Returns the desired normalized movement direction for this FixedUpdate.
    /// </summary>
    public Vector2 ComputeDirection(Vector2 toTargetDir, float distanceToTarget, bool allowStrafe = true)
    {
        if (toTargetDir.sqrMagnitude < 0.0001f)
            toTargetDir = lastOutputDir.sqrMagnitude > 0.0001f ? lastOutputDir : Vector2.up;

        Vector2 origin = SteeringOrigin;
        Vector2 targetPos = origin + toTargetDir * distanceToTarget;

        // Line-of-sight test used for both strafe gating and leave-wall decisions.
        bool losClear = !HasObstacleBetween(origin, targetPos);

        Vector2 desired;

        // Close-range strafe pre-empts long-range navigation.
        if (allowStrafe && distanceToTarget <= strafeRange && distanceToTarget > 0.01f && losClear)
        {
            desired = RunStrafe(toTargetDir, origin);
            ResetWallState();
        }
        else
        {
            switch (state)
            {
                case BugState.WallFollow:
                    desired = RunWallFollow(toTargetDir, distanceToTarget, origin, losClear);
                    break;
                default:
                case BugState.Seek:
                    desired = RunSeek(toTargetDir, distanceToTarget, origin);
                    break;
            }
        }

        if (desired.sqrMagnitude < 0.0001f)
            desired = lastOutputDir.sqrMagnitude > 0.0001f ? lastOutputDir : Vector2.up;

        Vector2 output = SmoothDirection(desired.normalized, lastOutputDir, directionSmoothing, 0.5f);
        lastOutputDir = output;

#if UNITY_EDITOR
        _debugOrigin = origin;
        _debugTargetPos = targetPos;
        _debugState = state;
        _debugWallSide = wallSide;
        _debugLOS = losClear;
        _debugWallTimer = wallFollowTimer;
        // _debugSideHit/_debugSideDist/_debugSideNormal are written inside RunWallFollow.
        _debugOutput = output;
#endif

        return output;
    }

    // ---------------------------------------------------------------
    // Seek
    // ---------------------------------------------------------------
    Vector2 RunSeek(Vector2 toTargetDir, float distanceToTarget, Vector2 origin)
    {
        // Soft arrival slowdown.
        float arrival = distanceToTarget < arrivalPause
            ? distanceToTarget / arrivalPause
            : 1f;
        Vector2 dir = toTargetDir * Mathf.Max(arrival, 0.1f);

        // Is the straight path blocked before we reach the target?
        float probeRadius = bodyRadius * 0.9f;
        RaycastHit2D hit = Physics2D.CircleCast(origin, probeRadius, toTargetDir, forwardSensorLength, obstacleMask);
        if (hit.collider && hit.distance < distanceToTarget - 0.05f)
        {
            // Transition to wall-follow at the point of impact.
            state = BugState.WallFollow;
            hitPoint = origin + toTargetDir * hit.distance;
            wallSide = PickWallSide(toTargetDir, hit.normal);
            wallFollowTimer = 0f;

            // Start by aligning with the wall tangent on the chosen side.
            Vector2 initialTangent = Perpendicular(hit.normal, wallSide).normalized;
            return initialTangent;
        }

        return dir;
    }

    // ---------------------------------------------------------------
    // Wall Follow (Bug2 boundary tracing)
    // ---------------------------------------------------------------
    Vector2 RunWallFollow(Vector2 toTargetDir, float distanceToTarget, Vector2 origin, bool losClear)
    {
        wallFollowTimer += Time.fixedDeltaTime;

        // Side whisker points into the committed wall side.
        Vector2 heading = lastOutputDir;
        Vector2 sideDir = Rotate(heading, -wallSide * 90f);
        float sideProbeRadius = bodyRadius * 0.5f;
        RaycastHit2D sideHit = Physics2D.CircleCast(origin, sideProbeRadius, sideDir, sideSensorLength, obstacleMask);

        Vector2 desired;

        if (sideHit.collider)
        {
            Vector2 n = sideHit.normal;                 // wall -> agent
            Vector2 tangent = Perpendicular(n, wallSide).normalized;
            float error = sideHit.distance - sideTargetOffset;
            desired = (tangent - n * (error * sideSeekCoeff)).normalized;

#if UNITY_EDITOR
            _debugSideHit = true;
            _debugSideDist = sideHit.distance;
            _debugSideNormal = n;
#endif
        }
        else
        {
            // Wall ended on this side (concave corner or wall turn-away); curve back toward it.
            desired = Rotate(heading, -wallSide * cornerTurnRate * Time.fixedDeltaTime).normalized;

#if UNITY_EDITOR
            _debugSideHit = false;
            _debugSideDist = sideSensorLength;
            _debugSideNormal = Vector2.zero;
#endif
        }

        // Forward probe catches convex corners that protrude into our path.
        float forwardProbeRadius = bodyRadius * 0.8f;
        float forwardProbeLength = bodyRadius * 2f;
        RaycastHit2D forwardHit = Physics2D.CircleCast(origin, forwardProbeRadius, heading, forwardProbeLength, obstacleMask);
        if (forwardHit.collider)
        {
            desired = Rotate(desired, -wallSide * cornerTurnRate * Time.fixedDeltaTime).normalized;
        }

        if (losClear || wallFollowTimer > maxWallFollowTime)
        {
            state = BugState.Seek;
            ResetWallState();
            return toTargetDir;
        }

        return desired;
    }

    void ResetWallState()
    {
        wallFollowTimer = 0f;
        state = BugState.Seek;
    }

    // ---------------------------------------------------------------
    // Strafe
    // ---------------------------------------------------------------
    Vector2 RunStrafe(Vector2 toTargetDir, Vector2 origin)
    {
        Vector2 perpR = Perpendicular(toTargetDir, 1);
        Vector2 perpL = Perpendicular(toTargetDir, -1);

        float clearR = CastDistance(origin, perpR, strafeRange);
        float clearL = CastDistance(origin, perpL, strafeRange);

        float timeSinceFlip = Time.time - lastStrafeFlipTime;
        if (timeSinceFlip >= STRAFE_STICK_TIME)
        {
            float currentClear = strafeSign > 0 ? clearR : clearL;
            float otherClear = strafeSign > 0 ? clearL : clearR;

            if (otherClear > currentClear + 0.2f)
            {
                strafeSign = -strafeSign;
                lastStrafeFlipTime = Time.time;
            }
        }

        // Emergency flip if the current orbit direction runs into a wall soon.
        // If both directions are blocked, fall back to moving toward the player instead of orbiting.
        Vector2 currentOrbitDir = Perpendicular(toTargetDir, strafeSign);
        float currentDirClear = CastDistance(origin, currentOrbitDir, strafeRange);
        if (currentDirClear < strafeRange * 0.5f)
        {
            strafeSign = -strafeSign;
            lastStrafeFlipTime = Time.time;

            Vector2 otherOrbitDir = Perpendicular(toTargetDir, strafeSign);
            float otherDirClear = CastDistance(origin, otherOrbitDir, strafeRange);
            if (otherDirClear < strafeRange * 0.5f)
                return toTargetDir;
        }

        return Perpendicular(toTargetDir, strafeSign);
    }

    // ---------------------------------------------------------------
    // Sensing helpers
    // ---------------------------------------------------------------
    bool HasObstacleBetween(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.001f) return false;
        dir.Normalize();

        float radius = bodyRadius * 0.7f;
        RaycastHit2D hit = Physics2D.CircleCast(from, radius, dir, dist, obstacleMask);
        return hit.collider != null;
    }

    int PickWallSide(Vector2 toGoalDir, Vector2 hitNormal)
    {
        // There are two valid wall-follow tangents. Choose the one that points more
        // toward the goal so the agent goes around the obstacle toward the player.
        // Perpendicular(n, +1) is the tangent for right-hand follow (wallSide = +1).
        // Perpendicular(n, -1) is the tangent for left-hand follow  (wallSide = -1).
        Vector2 rightTangent = Perpendicular(hitNormal, +1);
        Vector2 leftTangent  = Perpendicular(hitNormal, -1);

        float dotR = Vector2.Dot(rightTangent, toGoalDir);
        float dotL = Vector2.Dot(leftTangent,  toGoalDir);

        if (dotR > dotL + 0.01f) return +1;
        if (dotL > dotR + 0.01f) return -1;
        return +1; // tiebreak right-hand follow
    }

    float CastDistance(Vector2 origin, Vector2 dir, float length)
    {
        return CastClearDistance(origin, dir, length, bodyRadius * 0.8f);
    }

    float CastClearDistance(Vector2 origin, Vector2 dir, float length, float radius)
    {
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, dir, length, obstacleMask);
        return hit.collider ? hit.distance : length;
    }

    // ---------------------------------------------------------------
    // Math helpers
    // ---------------------------------------------------------------
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

    // ---------------------------------------------------------------
    // Debug gizmos
    // ---------------------------------------------------------------
#if UNITY_EDITOR
    Vector2 _debugOrigin;
    Vector2 _debugTargetPos;
    BugState _debugState;
    int _debugWallSide;
    bool _debugLOS;
    float _debugWallTimer;
    bool _debugSideHit;
    float _debugSideDist;
    Vector2 _debugSideNormal;
    Vector2 _debugOutput;

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 pos = _debugOrigin;
        Vector3 targetPos = _debugTargetPos;

        // LOS ray.
        Gizmos.color = _debugLOS ? new Color(1f, 1f, 1f, 0.75f) : new Color(1f, 0f, 0f, 0.75f);
        Gizmos.DrawLine(pos, targetPos);

        // Output direction.
        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        Gizmos.DrawRay(pos, (Vector3)_debugOutput * forwardSensorLength);

        if (_debugState == BugState.Seek)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.75f);
            Gizmos.DrawRay(pos, ((Vector3)targetPos - pos).normalized * forwardSensorLength);
        }
        else if (_debugState == BugState.WallFollow)
        {
            // Side whisker.
            Vector2 heading = _debugOutput.sqrMagnitude > 0.0001f ? _debugOutput : Vector2.up;
            Vector2 sideDir = Rotate(heading, -_debugWallSide * 90f);
            Gizmos.color = _debugSideHit ? new Color(0f, 0.5f, 1f, 0.85f) : new Color(0f, 0.5f, 1f, 0.35f);
            Gizmos.DrawRay(pos, (Vector3)(sideDir * _debugSideDist));

            // Forward probe.
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawRay(pos, (Vector3)(heading * bodyRadius * 2f));

            // Recorded hit point.
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere((Vector3)hitPoint, 0.15f);
            Gizmos.DrawLine(pos, (Vector3)hitPoint);
        }

        // Strafe range.
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, strafeRange);

        // State label.
        Handles.Label(pos + Vector3.up * 1.5f,
            $"State: {_debugState}\nSide: {_debugWallSide}\nLOS: {_debugLOS}\nWallT: {_debugWallTimer:F1}s");
    }
#endif
}
