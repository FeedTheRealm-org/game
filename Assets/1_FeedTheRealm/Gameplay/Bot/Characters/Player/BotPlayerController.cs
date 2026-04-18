using System.Collections;
using FTR.Core.Bot.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Gameplay.Bot.Characters.Player
{
    public class BotPlayerController : MonoBehaviour
    {
        private NetworkAdapter networkAdapter;
        private BotRuntimeConfig runtimeConfig;

        private bool isInitialized;
        private Vector3 moveDirection;
        private Coroutine directionRoutine;
        private Coroutine moveRoutine;
        private Coroutine actionRoutine;
        private Coroutine interactRoutine;

        public void Initialize(NetworkAdapter networkAdapter, BotRuntimeConfig runtimeConfig)
        {
            this.networkAdapter = networkAdapter;
            this.runtimeConfig = runtimeConfig;

            isInitialized = true;
            moveDirection = GetRandomMoveDirection();
            StartBehaviorRoutines();
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
            isInitialized = true;

            directionRoutine = StartCoroutine(DirectionLoop());
            moveRoutine = StartCoroutine(MoveLoop());
            actionRoutine = StartCoroutine(ActionLoop());
            interactRoutine = StartCoroutine(InteractLoop());
        }

        private void StopBehaviorRoutines()
        {
            isInitialized = false;
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

        private IEnumerator DirectionLoop()
        {
            var wait = new WaitForSeconds(
                Mathf.Max(0.25f, runtimeConfig.DirectionChangeIntervalSeconds)
            );

            while (isInitialized)
            {
                yield return wait;
                if (!CanDispatch())
                    continue;

                moveDirection = GetRandomMoveDirection();
            }
        }

        private IEnumerator MoveLoop()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.05f, runtimeConfig.MoveIntervalSeconds));

            while (isInitialized)
            {
                DispatchAction(ActionType.Move);
                yield return wait;
            }
        }

        private IEnumerator ActionLoop()
        {
            yield return new WaitForSeconds(0.5f);

            var wait = new WaitForSeconds(Mathf.Max(0.2f, runtimeConfig.ActionIntervalSeconds));

            while (isInitialized)
            {
                var actionType = UnityEngine.Random.value > 0.8f ? ActionType.Dash : ActionType.Use;
                DispatchAction(actionType);
                yield return wait;
            }
        }

        private IEnumerator InteractLoop()
        {
            yield return new WaitForSeconds(1.0f);

            var wait = new WaitForSeconds(Mathf.Max(0.5f, runtimeConfig.InteractIntervalSeconds));

            while (isInitialized)
            {
                DispatchAction(ActionType.Interact);

                if (UnityEngine.Random.value > 0.5f)
                    DispatchAction(ActionType.DialogNext);

                yield return wait;
            }
        }

        private bool CanDispatch()
        {
            return isInitialized && networkAdapter != null && networkAdapter.IsLocalPlayer;
        }

        private void DispatchAction(ActionType type)
        {
            if (!CanDispatch())
                return;

            networkAdapter.DispatchAction(
                new ActionCommandDTO
                {
                    Type = type,
                    NetId = networkAdapter.netId,
                    Direction = moveDirection,
                }
            );
        }

        private static Vector3 GetRandomMoveDirection()
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
