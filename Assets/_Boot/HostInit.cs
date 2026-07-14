using UnityEngine;
using Unity.Netcode;
#if UNITY_EDITOR
using ParrelSync;
#endif

public class HostInit : MonoBehaviour
{
    void Start()
    {
        bool isClone = false;
#if UNITY_EDITOR
        isClone = ClonesManager.IsClone();
#endif

        if (!isClone)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }
        }
    }
}