using UnityEngine;

[CreateAssetMenu(fileName = "WorldSelector", menuName = "Scriptable Objects/WorldSelector")]
public class WorldSelector : ScriptableObject
{
    [SerializeField]
    private string SelectedWorldId = "";

    public string GetSelectedWorldId() => SelectedWorldId;

    public void SetSelectedWorldId(string worldId) => SelectedWorldId = worldId;
}
