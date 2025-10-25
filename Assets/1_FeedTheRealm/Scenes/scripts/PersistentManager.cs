using UnityEngine;

public class PersistentManagers : MonoBehaviour
{
    public static PersistentManagers Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCoreSystems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCoreSystems()
    {
        // Sistemas que deben persistir entre escenas
        gameObject.AddComponent<NetworkSceneManager>();
        //gameObject.AddComponent<AudioManager>(); // Si necesitas audio persistente
    }
}