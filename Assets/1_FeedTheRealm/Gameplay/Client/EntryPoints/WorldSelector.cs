using UnityEngine;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// ScriptableObject used to store the selected world ID across different scenes and components in the client application.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldSelector", menuName = "Scriptable Objects/WorldSelector")]
    public class WorldSelector : ScriptableObject
    {
        [SerializeField]
        private string SelectedWorldId = "";

        public string GetSelectedWorldId() => SelectedWorldId;

        public void SetSelectedWorldId(string worldId) => SelectedWorldId = worldId;
    }
}
