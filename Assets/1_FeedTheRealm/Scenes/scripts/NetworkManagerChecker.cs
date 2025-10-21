using UnityEngine;
using Unity.Netcode;

public class NetworkManagerChecker : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Escena: {gameObject.scene.name}");
        Debug.Log($"NetworkManager existe: {NetworkManager.Singleton != null}");
        
        if (NetworkManager.Singleton != null)
        {
            Debug.Log($"NetworkManager scene: {NetworkManager.Singleton.gameObject.scene.name}");
        }
    }
}