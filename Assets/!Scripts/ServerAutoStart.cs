using FishNet;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Application.isBatchMode = true при запуске с аргументами -batchmode -nographics
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Headless mode detected. Starting server...");
            //(порт по умолчанию 7770)
            InstanceFinder.ServerManager.StartConnection();
        }
        else
        {
            Debug.Log("[Server] Not in batch mode – server not auto-started.");
        }
    }
}