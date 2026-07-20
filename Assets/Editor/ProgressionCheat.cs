using UnityEngine;
using UnityEditor;

public class ProgressionCheat : MonoBehaviour
{
    [MenuItem("Tools/Progression/Unlock All Levels")]
    public static void UnlockAllLevels()
    {
        PlayerPrefs.SetInt("MaxUnlockedLevel", 999);
        PlayerPrefs.Save();

    }

    [MenuItem("Tools/Progression/Reset Level Progress")]
    public static void ResetLevels()
    {
        PlayerPrefs.DeleteKey("MaxUnlockedLevel");
        PlayerPrefs.Save();

    }
}
