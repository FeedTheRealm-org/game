using System.Collections;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Environment.Npcs;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side system that authoritatively manages NPC dialog interactions.
    /// Handles starting, advancing, switching, and closing dialog sequences.
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

        [SerializeField]
        private float inactivityTimeout = 10f;

        private CharacterStateStorage stateStorage;
        private uint netId;
        private Coroutine _inactivityCoroutine;

        public void Initialize(uint netId, CharacterStateStorage stateStorage)
        {
            this.netId = netId;
            this.stateStorage = stateStorage;
        }

        public void OnInteract(IEventCollectable ec)
        {
            string closestNpcId = FindClosestNpcId();

            if (stateStorage.IsInteracting)
            {
                if (stateStorage.CurrentNpcId == closestNpcId)
                {
                    OnDialogNext(ec);
                    return;
                }

                StopInactivityTimer();
                stateStorage.SwitchInteractingNpc(closestNpcId);
                stateStorage.SetDialogIndex(0);
                RestartInactivityTimer();
                return;
            }

            if (string.IsNullOrEmpty(closestNpcId))
            {
                logger.Log("[InteractSystem] No NPC found within interaction range.", this);
                return;
            }

            int count = npcDialogRegistry.GetMessageCount(closestNpcId);
            if (count == 0)
            {
                logger.Log(
                    $"[InteractSystem] No messages registered for NpcId '{closestNpcId}'.",
                    this
                );
                return;
            }

            stateStorage.SetInteracting(true, closestNpcId);
            stateStorage.SetDialogIndex(0);
            RestartInactivityTimer();
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
            RestartInactivityTimer();

            ec.Collect(
                new DialogEvent(
                    netId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeAdvanced,
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

            StopInactivityTimer();
            stateStorage.SetInteracting(false);
        }

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

        private void RestartInactivityTimer()
        {
            StopInactivityTimer();
            _inactivityCoroutine = StartCoroutine(InactivityCoroutine());
        }

        private void StopInactivityTimer()
        {
            if (_inactivityCoroutine != null)
            {
                StopCoroutine(_inactivityCoroutine);
                _inactivityCoroutine = null;
            }
        }

        private IEnumerator InactivityCoroutine()
        {
            yield return new WaitForSeconds(inactivityTimeout);

            if (stateStorage.IsInteracting)
            {
                /*logger.Log(
                    $"[InteractSystem] Inactivity timeout — closing dialog for NPC '{stateStorage.CurrentNpcId}'.",
                    this
                );*/
                stateStorage.SetInteracting(false);
            }
        }
    }
}
