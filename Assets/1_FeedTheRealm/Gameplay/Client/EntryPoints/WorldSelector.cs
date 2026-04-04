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

        [SerializeField]
        private string SelectedWorldJoinToken = "";

        public string GetSelectedWorldId() => SelectedWorldId;

        public string GetSelectedWorldJoinToken() => SelectedWorldJoinToken;

        public void SetSelectedWorldId(string worldId) => SelectedWorldId = worldId;

        public void SetSelectedWorldJoinToken(string tokenId) => SelectedWorldJoinToken = tokenId;

        public void ClearSelectedWorldJoinToken() => SelectedWorldJoinToken = string.Empty;
    }
}
