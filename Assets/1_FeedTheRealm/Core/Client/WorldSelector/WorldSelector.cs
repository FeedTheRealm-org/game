using System;
using UnityEngine;

namespace FTR.Core.Client.EntryPoints
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
        private string SelectedWorldUpdatedAt = "";

        [SerializeField]
        private int SelectedZoneId = 1;

        [SerializeField]
        private string SelectedWorldJoinToken = "";

        public string GetSelectedWorldId() => SelectedWorldId;

        public DateTimeOffset GetSelectedWorldUpdatedAt()
        {
            try
            {
                return DateTimeHelper.ParseDateTimeOffset(SelectedWorldUpdatedAt);
            }
            catch (FormatException)
            {
                return DateTimeOffset.MinValue;
            }
        }

        public string GetSelectedWorldJoinToken() => SelectedWorldJoinToken;

        public void SetSelectedWorldId(string worldId) => SelectedWorldId = worldId;

        public void SetSelectedWorldUpdatedAt(string updatedAt) =>
            SelectedWorldUpdatedAt = updatedAt;

        public void SetSelectedWorldJoinToken(string tokenId) => SelectedWorldJoinToken = tokenId;

        public void ClearSelectedWorldJoinToken() => SelectedWorldJoinToken = string.Empty;

        public int GetSelectedZoneId() => SelectedZoneId;

        public void SetSelectedZoneId(int zoneId) => SelectedZoneId = zoneId;
    }
}
