using UnityEngine;

/// <summary>
/// Pure math helpers for context steering. Zero Physics2D, Time, Transform,
/// or any UnityEngine state — all inputs are values or arrays. Deterministic
/// and unit-testable.
/// </summary>
public static class SteeringMath
{
    public static Vector2[] BuildDirections(int count)
    {
        Vector2[] dirs = new Vector2[count];
        float anglePerStep = 360f / count * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            float angle = i * anglePerStep;
            dirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        return dirs;
    }

    /// <summary>Compute interest per compass slot: dot of each dir vs target direction, clamped [0,1].</summary>
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

    /// <summary>
    /// Spread a danger weight into a compass slot and its angular neighbors.
    /// falloffWeights[0] applies to distance 0 (same slot), [1] at ±1, etc.
    /// Each target slot is clamped to [0,1].
    /// </summary>
    public static void ApplyDangerFalloff(float[] danger, int slot, float weight, float[] falloffWeights, int dirCount)
    {
        int maxOffset = falloffWeights.Length - 1;
        for (int offset = -maxOffset; offset <= maxOffset; offset++)
        {
            float fw = falloffWeights[Mathf.Abs(offset)];
            if (fw <= 0f) continue;
            int idx = (slot + offset + dirCount) % dirCount;
            danger[idx] = Mathf.Min(1f, danger[idx] + weight * fw);
        }
    }

    /// <summary>Return the compass slot index nearest to the given direction.</summary>
    public static int GetNearestSlot(Vector2 dir, int dirCount)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        float anglePerSlot = 360f / dirCount;
        int slot = Mathf.RoundToInt(angle / anglePerSlot);
        return slot % dirCount;
    }

    /// <summary>
    /// Blend the best-scoring slot with its angular neighbors.
    /// Returns zero if all final &le; 0.
    /// </summary>
    public static Vector2 Blend(float[] final, Vector2[] dirs, float neighborAngleThreshold)
    {
        int count = final.Length;
        int bestIdx = 0;
        float bestVal = final[0];
        for (int i = 1; i < count; i++)
        {
            if (final[i] > bestVal) { bestVal = final[i]; bestIdx = i; }
        }
        if (bestVal <= 0f) return Vector2.zero;

        Vector2 blended = dirs[bestIdx] * bestVal;
        float totalWeight = bestVal;
        float anglePerSlot = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (i == bestIdx) continue;
            if (final[i] <= 0f) continue;
            int slotDiff = Mathf.Abs(i - bestIdx);
            if (slotDiff > count / 2) slotDiff = count - slotDiff;
            float angleDiff = slotDiff * anglePerSlot;
            if (angleDiff > neighborAngleThreshold) continue;

            float weight = final[i] * (1f - (angleDiff / neighborAngleThreshold));
            blended += dirs[i] * weight;
            totalWeight += weight;
        }

        return totalWeight > 0.0001f ? blended.normalized : Vector2.zero;
    }

    /// <summary>
    /// Return the direction of the slot with the least danger.
    /// Used as fallback when all final scores are ≤ 0 (fully boxed in).
    /// </summary>
    public static Vector2 LeastDangerDirection(float[] danger, Vector2[] dirs)
    {
        int best = 0;
        float minDanger = danger[0];
        for (int i = 1; i < danger.Length; i++)
        {
            if (danger[i] < minDanger)
            {
                minDanger = danger[i];
                best = i;
            }
        }
        return dirs[best];
    }

    /// <summary>
    /// Smooth the output direction by blending with the previous frame's output.
    /// factor &gt; 0 leans toward previous (0 = full smoothing, 1 = no smoothing).
    /// Skips smoothing when the turn angle exceeds opposeThreshold (cosine).
    /// </summary>
    public static Vector2 SmoothDirection(Vector2 current, Vector2 previous, float factor, float opposeThreshold)
    {
        if (previous.sqrMagnitude < 0.0001f) return current;
        if (current.sqrMagnitude < 0.0001f) return Vector2.zero;
        if (Vector2.Dot(current, previous) < -opposeThreshold) return current;
        return Vector2.Lerp(current, previous, factor).normalized;
    }

    /// <summary>Perpendicular vector: sign &ge; 0 → right, sign &lt; 0 → left.</summary>
    public static Vector2 Perpendicular(Vector2 v, int sign)
    {
        return sign >= 0 ? new Vector2(v.y, -v.x) : new Vector2(-v.y, v.x);
    }
}
