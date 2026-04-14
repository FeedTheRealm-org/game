using System.Collections;
using System.Collections.Generic;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Environment.Portal
{
    /// <summary>
    /// Portal represents a portal in the game world that can be interacted with by players.
    /// It is responsible for sending teleport commands to the server when a player interacts with it.
    /// </summary>
    public class PortalController : MonoBehaviour
    {
        [SerializeField]
        Logging.Logger logger;

        private string portalId;
        private uint netId;

        public void Initialize(uint netId, string portalId)
        {
            this.portalId = portalId;
            this.netId = netId;
        }

        private void OnTriggerEnter(Collider other)
        {
            Gizmos.color = Color.lightBlue;

            logger.Log(
                $"{other.gameObject.name} entered trigger of {gameObject.name} (ID: {portalId})."
            );

            var playerIdentity = other.GetComponentInParent<NetworkIdentity>();
            if (playerIdentity == null)
            {
                logger.Log(
                    $"No NetworkIdentity found on {other.gameObject.name} or its parents. Ignoring.",
                    this
                );
                return;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Gizmos.color = Color.purple;
        }

        private void OnDrawGizmos()
        {
            var radius = GetComponentInParent<SphereCollider>().radius;
            Gizmos.color = Color.purple;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
