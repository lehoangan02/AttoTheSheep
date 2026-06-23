using UnityEngine;

/// <summary>
/// Empty marker component. Place on GameObjects in the scene to mark
/// valid enemy spawn positions for a WaveData asset.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private float gizmoRadius = 0.35f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
