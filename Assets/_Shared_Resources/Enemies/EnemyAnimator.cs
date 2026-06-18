using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    Animator animator;
    Dictionary<string, int> paramCache = new Dictionary<string, int>();

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    int GetParam(string name)
    {
        if (!paramCache.TryGetValue(name, out int hash))
        {
            hash = Animator.StringToHash(name);
            paramCache[name] = hash;
        }
        return hash;
    }

    public void PlayIdle() => SetChasing(false);
    public void PlayRun() => SetChasing(true);
    public void SetChasing(bool isChasing) => Trigger("IsChasing", isChasing);

    public void PlayAttack(string triggerName = "Attack")
    {
        Trigger(triggerName);
    }

    public void PlayGuard()
    {
        Trigger("Guard");
    }

    public void PlayCast(string triggerName)
    {
        Trigger(triggerName);
    }

    public void PlaySkill(string triggerName)
    {
        Trigger(triggerName);
    }

    void Trigger(string name)
    {
        if (animator != null)
            animator.SetTrigger(GetParam(name));
    }

    void Trigger(string name, bool value)
    {
        if (animator != null)
            animator.SetBool(GetParam(name), value);
    }
}
