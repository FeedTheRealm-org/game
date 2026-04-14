using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Portal
{
    /// <summary>
    /// Lightweight registry that stores only the set of valid quest ids from world data.
    /// Used server-side to validate AcceptQuest commands without holding full QuestData in memory.
    /// </summary>
    [CreateAssetMenu(fileName = "PortalRegistry", menuName = "Scriptable Objects/PortalRegistry")]
    public class PortalRegistry : ScriptableObject
    {
        private Dictionary<string, PortalInformation> registeredPortals;

        public bool TryGetPortal(string portalId, out PortalInformation portalInfo)
        {
            if (registeredPortals == null)
            {
                Debug.LogError(
                    "PortalRegistry not populated. Call Populate() with world data before using."
                );
                portalInfo = null;
                return false;
            }

            return registeredPortals.TryGetValue(portalId, out portalInfo);
        }

        public void Populate(
            List<PortalData> portalData,
            List<PortalPlacementData> portalPlacementData
        )
        {
            registeredPortals = new Dictionary<string, PortalInformation>();

            // Create a lookup for portalData for quick access
            var portalDataLookup = new Dictionary<string, PortalData>();
            foreach (var portal in portalData)
            {
                if (!string.IsNullOrEmpty(portal.id) && !portalDataLookup.ContainsKey(portal.id))
                {
                    portalDataLookup[portal.id] = portal;
                }
            }

            int successCount = 0;

            // Iterate through portalPlacementData and create PortalInformation objects
            foreach (var placement in portalPlacementData)
            {
                if (string.IsNullOrEmpty(placement.id))
                {
                    Debug.LogWarning("Portal placement with empty id found. Skipping.");
                    continue;
                }

                if (registeredPortals.ContainsKey(placement.id))
                {
                    Debug.LogWarning(
                        $"Duplicate portal placement id {placement.id} found. Skipping."
                    );
                    continue;
                }

                var portalInfo = new PortalInformation { PlacementData = placement };

                // Try to find corresponding portal data
                if (!portalDataLookup.TryGetValue(placement.id, out var portalDataEntry))
                    throw new System.ArgumentException(
                        $"No portal data found for placement {placement.id}."
                    );
                portalInfo.Data = portalDataEntry;

                registeredPortals[placement.id] = portalInfo;

                // Log if complete or incomplete
                if (portalInfo.IsComplete)
                    successCount++;
                else
                    Debug.LogWarning($"Portal {placement.id} is incomplete.");
            }
            Debug.Log(
                $"Successfully registered {successCount}/{portalPlacementData.Count} portals."
            );
        }
    }

    public class PortalInformation
    {
        private PortalData _data;
        private PortalPlacementData _placementData;
        private bool _isComplete;

        public PortalData Data
        {
            get => _data;
            set
            {
                _data = value;
                UpdateCompletion();
            }
        }

        public PortalPlacementData PlacementData
        {
            get => _placementData;
            set
            {
                _placementData = value;
                UpdateCompletion();
            }
        }

        public bool IsComplete => _isComplete;

        private void UpdateCompletion()
        {
            _isComplete = _data != null && _placementData != null && _data.id == _placementData.id;
        }
    }
}
