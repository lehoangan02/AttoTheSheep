using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteractionContext
{
    public static event System.Action<bool> AnyInRangeChanged;
    public static bool AnyInRange => s_count > 0;

    private static int s_count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad() => Reset();

    public static void EnterRange()
    {
        s_count++;
        if (s_count == 1) AnyInRangeChanged?.Invoke(true);
    }

    public static void ExitRange()
    {
        if (s_count > 0) s_count--;
        if (s_count == 0) AnyInRangeChanged?.Invoke(false);
    }

    public static void Reset()
    {
        s_count = 0;
        AnyInRangeChanged?.Invoke(false);
    }
}
