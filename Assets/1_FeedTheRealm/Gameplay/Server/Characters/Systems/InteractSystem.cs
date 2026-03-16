using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Environment.Npcs;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side system that authoritatively manages NPC dialog interactions.
    ///
    /// This system finds the closest NpcIdentity within range from the player's
    /// authoritative position.
    /// </summary>
    public class InteractSystem : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private float interactionRadius = 2.5f;

        [SerializeField]
        private LayerMask npcLayerMask;

        private CharacterStateStorage stateStorage;
        private uint netId;

        public void Initialize(uint netId, CharacterStateStorage stateStorage)
        {
            this.netId = netId;
            this.stateStorage = stateStorage;
        }

        public void OnInteract(IEventCollectable ec)
        {
            if (stateStorage.IsInteracting)
            {
                logger.Log(
                    $"[InteractSystem] Already interacting with '{stateStorage.CurrentNpcId}'.",
                    this
                );
                return;
            }

            string npcId = FindClosestNpcId();
            if (string.IsNullOrEmpty(npcId))
            {
                logger.Log("[InteractSystem] No NPC found within interaction range.", this);
                return;
            }

            int count = npcDialogRegistry.GetMessageCount(npcId);
            if (count == 0)
            {
                logger.Log($"[InteractSystem] No messages registered for NpcId '{npcId}'.", this);
                return;
            }

            stateStorage.SetInteracting(true, npcId);
            stateStorage.SetDialogIndex(0);
        }

        /// <summary>
        /// Advances the dialog index or closes the sequence if the last message was shown.
        /// </summary>
        public void OnDialogNext(IEventCollectable ec)
        {
            if (!stateStorage.IsInteracting)
            {
                logger.Log("[InteractSystem] DialogNext received but not interacting.", this);
                return;
            }

            var npcId = stateStorage.CurrentNpcId;
            int count = npcDialogRegistry.GetMessageCount(npcId);

            if (count == 0)
            {
                CloseDialog(ec);
                return;
            }

            int nextIndex = stateStorage.CurrentDialogIndex + 1;

            if (nextIndex >= count)
            {
                CloseDialog(ec);
                return;
            }

            stateStorage.SetDialogIndex(nextIndex);

            ec.Collect(
                new DialogEvent(
                    netId,
                    new DialogEventContent
                    {
                        DialogState = (int)DialogState.Advanced,
                        NpcId = npcId,
                        DialogIndex = nextIndex,
                    }
                )
            );
        }

        /// <summary>
        /// Closes the current dialog unconditionally.
        /// </summary>
        public void CloseDialog(IEventCollectable ec)
        {
            if (!stateStorage.IsInteracting)
                return;

            stateStorage.SetInteracting(false);
        }

        /// <summary>
        /// Finds the closest NpcIdentity within interactionRadius using the player's
        /// authoritative position from CharacterStateStorage.
        /// </summary>
        private string FindClosestNpcId()
        {
            var playerPos = stateStorage.Position;

            Collider[] hits = Physics.OverlapSphere(playerPos, interactionRadius, npcLayerMask);

            NpcIdentity closest = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var identity = hit.GetComponent<NpcIdentity>();
                if (identity == null)
                    continue;

                float dist = Vector3.Distance(playerPos, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = identity;
                }
            }

            return closest != null ? closest.NpcId : null;
        }
    }
}
