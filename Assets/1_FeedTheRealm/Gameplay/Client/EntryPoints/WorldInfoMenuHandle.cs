using UnityEngine;

public class WorldInfoMenuHandle
{
    public GameObject Instance { get; private set; }

    public void SetInstance(GameObject instance)
    {
        Instance = instance;
    }
}
