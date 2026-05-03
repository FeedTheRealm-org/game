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
    /// The portal controller is responsible for handling the portal's trigger events and executing teleportation logic.
    /// Currently it only just renders the portal's trigger area and logs trigger events
    /// </summary>
    public class PortalController : MonoBehaviour
    {
        [SerializeField]
        Logging.Logger logger;
        private uint netId;

        public void Initialize(uint netId)
        {
            this.netId = netId;
        }

        private void OnTriggerEnter(Collider other)
        {
            Gizmos.color = Color.blue;

            logger.Log(
                $"{other.gameObject.name} entered trigger of {gameObject.name} (ID: {netId})."
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
            var scale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, scale);
            Gizmos.DrawWireSphere(Vector3.zero, radius);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
