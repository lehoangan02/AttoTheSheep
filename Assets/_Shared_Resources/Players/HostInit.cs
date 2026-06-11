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
                NetworkManager.Singleton.StartHost();
            }
        #endif
    }
}