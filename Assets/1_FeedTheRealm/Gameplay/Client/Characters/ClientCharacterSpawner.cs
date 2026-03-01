using UnityEngine;

[CreateAssetMenu(
    fileName = "ClientCharacterSpawner",
    menuName = "Feed The Realm/Client Character Spawner"
)]
public class ClientCharacterSpawner : ScriptableObject
{
    [SerializeField]
    private GameObject characterPrefab;

    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private int maxCharactersToSpawn = 1;

    private int spawnedCount = 0;

    public GameObject SpawnCharacter()
    {
        if (spawnedCount >= maxCharactersToSpawn)
        {
            Debug.LogWarning("Max characters already spawned!");
            return null;
        }

        if (characterPrefab == null)
        {
            Debug.LogError("Character prefab is not assigned!");
            return null;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject spawnedCharacter = Instantiate(characterPrefab, position, rotation);
        spawnedCount++;

        return spawnedCharacter;
    }

    public void ResetSpawner()
    {
        spawnedCount = 0;
    }

    public int GetSpawnedCount() => spawnedCount;
}
