using UnityEngine;

/// <summary>
/// Trigger zone attached to a Collider2D. When the player enters,
/// requests LevelManager to start the linked wave.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaveTrigger : MonoBehaviour
{
    [Header("Link")]
    [SerializeField] private WaveController targetWave;

    [Header("Visual")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.6f, 1f, 0.3f);

    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (targetWave == null)

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var netMgr = Unity.Netcode.NetworkManager.Singleton;
        if (netMgr != null && netMgr.IsListening && !netMgr.IsServer) return;

        if (!other.CompareTag("Player")) return;
        if (targetWave == null) return;
        if (LevelManager.Instance == null) return;

        LevelManager.Instance.TryStartWave(targetWave);
    }

    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = gizmoColor;

        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawSphere(circle.offset, circle.radius);
        }
    }
}
