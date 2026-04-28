using UnityEngine;

namespace FTR.Gameplay.Common.Characters.Shared.Portal
{
    /// <summary>
    /// This is a temporary ScriptableObject used to persist teleportation data across scene loads during the current teleportation refactor.
    ///  It will be removed once we have a more permanent solution for teleportation data persistence that is not reliant on the
    ///  client holding teleportation data in memory.
    /// </summary>
    [CreateAssetMenu(menuName = "Scriptable Objects/Client/Teleport Data Persistence")]
    public class TeleportDataPersistence : ScriptableObject
    {
        public string PortalId = null;
    }
}
