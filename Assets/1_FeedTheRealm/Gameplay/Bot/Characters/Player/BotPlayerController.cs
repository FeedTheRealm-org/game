using System.Collections;
using FTR.Core.Bot.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Bot.Characters.Player
{
    public class BotPlayerController : MonoBehaviour
    {
        [Inject]
        private Logging.Logger logger;

        [Inject]
        private BotConfig botConfig;

        private NetworkAdapter networkAdapter;

        private Vector3 moveDirection;
        private Coroutine directionRoutine;
        private Coroutine moveRoutine;
        private Coroutine actionRoutine;
        private Coroutine interactRoutine;

        public void Initialize(NetworkAdapter networkAdapter)
        {
            this.networkAdapter = networkAdapter;

            moveDirection = GetRandomMoveDirection();
            StartBehaviorRoutines();
            logger.Log(
                $"[BotPlayerController] Bot player controller initialized. botId={botConfig.BotId}, worldId={botConfig.WorldId}, zoneId={botConfig.ZoneId}, netId={networkAdapter.netId}"
            );
        }

        private void OnDisable()
        {
            StopBehaviorRoutines();
        }

        private void OnDestroy()
        {
            StopBehaviorRoutines();
        }

        private void StartBehaviorRoutines()
        {
            StopBehaviorRoutines();

            directionRoutine = StartCoroutine(DirectionLoop());
            moveRoutine = StartCoroutine(MoveLoop());
            actionRoutine = StartCoroutine(ActionLoop());
            interactRoutine = StartCoroutine(InteractLoop());
        }

        private void StopBehaviorRoutines()
        {
            StopRoutine(ref directionRoutine);
            StopRoutine(ref moveRoutine);
            StopRoutine(ref actionRoutine);
            StopRoutine(ref interactRoutine);
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine == null)
                return;

            StopCoroutine(routine);
            routine = null;
        }

        /* --- Behaviour Routines --- */

        private IEnumerator DirectionLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(botConfig.DirectionChangeIntervalSeconds);
                moveDirection = GetRandomMoveDirection();
            }
        }

        private IEnumerator MoveLoop()
        {
            while (true)
            {
                DispatchAction(ActionType.Move);
                yield return new WaitForSeconds(botConfig.MoveIntervalSeconds);
            }
        }

        private IEnumerator ActionLoop()
        {
            while (true)
            {
                var actionType = UnityEngine.Random.value > 0.5f ? ActionType.Dash : ActionType.Use;
                DispatchAction(actionType);
                yield return new WaitForSeconds(botConfig.ActionIntervalSeconds);
            }
        }

        private IEnumerator InteractLoop()
        {
            while (true)
            {
                DispatchAction(ActionType.Interact);

                if (UnityEngine.Random.value > 0.5f)
                    DispatchAction(ActionType.DialogNext);

                yield return new WaitForSeconds(botConfig.InteractIntervalSeconds);
            }
        }

        /* --- Utils --- */

        private void DispatchAction(ActionType type)
        {
            networkAdapter.DispatchAction(
                new ActionCommandDTO
                {
                    Type = type,
                    NetId = networkAdapter.netId,
                    Direction = moveDirection,
                }
            );
        }

        private Vector3 GetRandomMoveDirection()
        {
            var direction = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0f,
                UnityEngine.Random.Range(-1f, 1f)
            );

            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }
    }
}
