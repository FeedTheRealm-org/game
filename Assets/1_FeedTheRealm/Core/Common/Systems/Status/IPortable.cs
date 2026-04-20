using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.Systems.Status
{
    /// <summary>
    /// Interface that should be implemented by any component that wants to be notified when a character enters or exits a portal.
    /// </summary>
    public interface IPortable
    {
        public void OnEnterPortal(
            GameObject character,
            PortalData portalData,
            PortalPlacementData placementData
        );

        public void OnExitPortal(GameObject character);
    }
}
