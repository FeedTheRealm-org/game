using System;
using FTR.Core.Common.Systems.Status;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.Portal
{
    public class PortalStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnPortalIdSync))]
        private string portalId;

        /* --- Callbacks --- */
        public event Action<string> OnPortalIdChanged;
        public string PortalId => portalId;

        /* --- Syncvar hooks --- */

        private void OnPortalIdSync(string oldId, string newId)
        {
            OnPortalIdChanged?.Invoke(newId);
        }
    }
}
