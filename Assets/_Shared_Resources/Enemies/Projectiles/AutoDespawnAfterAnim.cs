using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class AutoDespawnAfterAnim : MonoBehaviour
{
    [SerializeField] private float maxLifetime = 2f;

    private void OnEnable()
    {
        StartCoroutine(DespawnAfterAnimRoutine());
    }

    private IEnumerator DespawnAfterAnimRoutine()
    {
        Animator anim = GetComponent<Animator>();
        float elapsed = 0f;

        while (elapsed < maxLifetime)
        {
            if (anim != null)
            {
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.length > 0f && stateInfo.normalizedTime >= 1f)
                    break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (TryGetComponent<NetworkObject>(out NetworkObject no) && no.IsSpawned)
            no.Despawn(true);
        else
            Destroy(gameObject);
    }
}
