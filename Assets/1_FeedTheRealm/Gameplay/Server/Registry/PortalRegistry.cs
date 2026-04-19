using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Registry
{
    //TODO: this can be refactored into a static class since we load the scene again when changing zones.
    /// <summary>
    /// Lightweight registry that stores only the set of valid quest ids from world data.
    /// Used server-side to validate Acce|tQuest commands without holding full QuestData in memory.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PortalRegistry",
        menuName = "Scriptable Objects/Server/PortalRegistry"
    )]
    public class PortalRegistry : ScriptableObject
    {
        private Dictionary<string, PortalPlacementData> portalPlacementDataLookup;
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

        public List<string> GetAllPortalIds()
        {
            if (registeredPortals == null)
            {
                Debug.LogError(
                    "PortalRegistry not populated. Call Populate() with world data before using."
                );
                return new List<string>();
            }
            return new List<string>(registeredPortals.Keys);
        }

        public void Populate(
            List<PortalData> portalData,
            List<PortalPlacementData> portalPlacementData
        )
        {
            registeredPortals = new Dictionary<string, PortalInformation>();

            PopulateLookup(portalPlacementData);

            RegisterPortalInfo(portalData);
        }

        private void PopulateLookup(List<PortalPlacementData> portalPlacementData)
        {
            portalPlacementDataLookup = new Dictionary<string, PortalPlacementData>();
            foreach (var placement in portalPlacementData)
            {
                if (
                    !string.IsNullOrEmpty(placement.id)
                    && !portalPlacementDataLookup.ContainsKey(placement.id)
                )
                {
                    portalPlacementDataLookup[placement.id] = placement;
                }
            }
        }

        private void RegisterPortalInfo(List<PortalData> portalData)
        {
            foreach (PortalData portal in portalData)
            {
                if (string.IsNullOrEmpty(portal.id))
                {
                    Debug.LogWarning("Portal with empty id found. Skipping.");
                    continue;
                }

                if (registeredPortals.ContainsKey(portal.id))
                {
                    Debug.LogWarning($"Duplicate portal id {portal.id} found. Skipping.");
                    continue;
                }

                var portalInfo = new PortalInformation { Data = portal };

                // Try to find corresponding placement data
                // For teleports in the same zone, we know its locations
                // For teleports in between zones, we know the destination ID, but we won't know the location until the player loads
                // the new zone where the destination portal is located.
                if (portalPlacementDataLookup.TryGetValue(portal.id, out var portalPlacementEntry))
                {
                    portalInfo.PlacementData = portalPlacementEntry;
                }
                registeredPortals[portal.id] = portalInfo;
            }
        }
    }

    public class PortalInformation
    {
        private PortalData _data;
        private PortalPlacementData _placementData;
        public string Name => _data?.name ?? "Unknown Portal";
        public string Id => _data?.id ?? "Unknown Portal ID";
        public string DestinationId => _data?.targetPortalId ?? "Unknown Destination";
        public int ZoneId => _data?.zoneId ?? -1;
        public Vector3 Position => _placementData?.position ?? Vector3.zero;

        public bool IsInPortalRadius(Vector3 playerPosition)
        {
            if (_placementData == null)
            {
                Debug.LogWarning(
                    $"Portal {Id} does not have placement data. Cannot determine if player is within radius."
                );
                return false;
            }

            float distance = Vector3.Distance(playerPosition, _placementData.position);
            return distance <= _placementData.radius;
        }

        public PortalData Data
        {
            set { _data = value; }
        }

        public PortalPlacementData PlacementData
        {
            set { _placementData = value; }
        }

        public override string ToString()
        {
            return $"PortalInformation: Id={Id}, Name={Name}, DestinationId={DestinationId}, ZoneId={ZoneId}, Position={Position}";
        }
    }
}
