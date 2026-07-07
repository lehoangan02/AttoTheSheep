using UnityEngine;
using Unity.Netcode;
using ParrelSync;

public class HostInit : MonoBehaviour
{
    void Start()
    {
        #if UNITY_EDITOR
            if (!ClonesManager.IsClone())
            {
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.StartHost();
                }
            }
        #endif
    }
}