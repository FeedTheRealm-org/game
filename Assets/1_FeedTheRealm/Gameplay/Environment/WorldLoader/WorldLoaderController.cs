using UnityEngine;
using World;

public class WorldLoader : MonoBehaviour {

    [Header("World Loader Settings")]
    [SerializeField] private string worldId;

    [Header("World Loader Requirements")]

    [SerializeField] private WorldController world;

    [SerializeField] private Worlds.WorldHandler worldsData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake() {







    }
}
