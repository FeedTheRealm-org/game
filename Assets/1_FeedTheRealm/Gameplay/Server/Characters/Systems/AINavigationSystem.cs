using System.Collections;
using FTR.Core.Server;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class AINavigationSystem : MonoBehaviour
    {
        [Inject]
        private ServerConfig config;

        [Inject]
        private Logging.Logger logger;

        [Inject]
        private GameTickEvent gameTickEvent;

        private enum AIState
        {
            Idle,
            Wandering,
            Chasing,
        }

        private AIState currentState = AIState.Idle;
        private NavMeshPath currentPath;
        private int currentPathIndex;
        private Vector3 lastSentDirection = Vector3.zero;

        private WorldMonitor worldMonitor;
        private CharacterStateStorage stateStorage;
        private uint netId;
        private Vector3 spawnCenter;
        private bool isInitialized;

        public void Initialize(
            uint netId,
            WorldMonitor worldMonitor,
            CharacterStateStorage stateStorage
        )
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
            spawnCenter = transform.root.position;
            this.stateStorage = stateStorage;

            currentPath = new NavMeshPath();

            gameTickEvent.OnRaised += GameTick;

            isInitialized = true;

            TransitionTo(AIState.Idle);
        }

        private void OnDisable()
        {
            gameTickEvent.OnRaised -= GameTick;
        }

        private void GameTick(float dt)
        {
            if (!isInitialized || currentState != AIState.Wandering)
                return;

            ProcessMovementAlongPath();
        }

        // Flattens a Vector3 to XZ plane for ground-distance checks
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void TransitionTo(AIState next)
        {
            currentState = next;

            switch (next)
            {
                case AIState.Idle:
                    StopAllCoroutines();
                    SendMove(Vector3.zero);
                    StartCoroutine(IdleRoutine());
                    break;

                case AIState.Wandering:
                    break;
            }
        }

        // Waits a random duration then tries to pick a new wander target.
        private IEnumerator IdleRoutine()
        {
            yield return new WaitForSeconds(Random.Range(config.MinWaitTime, config.MaxWaitTime));
            TryBeginWander();
        }

        public void OnChaseStart(Collider target)
        {
            // Chase behavior not implemented yet, so this is ignored.
        }

        public void OnChaseStop(Collider target)
        {
            // Chase behavior not implemented yet, so this is ignored.
        }

        private void TryBeginWander()
        {
            if (!stateStorage.IsGrounded)
            {
                logger.Log(
                    $"[AINav] {gameObject.name} not grounded, deferring wander.",
                    this,
                    Logging.LogType.Warning
                );
                TransitionTo(AIState.Idle);
                return;
            }

            Vector3 rootPos = transform.root.position;
            Vector3 randomTarget = spawnCenter + Random.insideUnitSphere * config.WanderRadius;

            bool startOnMesh = NavMesh.SamplePosition(
                rootPos,
                out NavMeshHit startHit,
                20f,
                NavMesh.AllAreas
            );
            bool targetOnMesh = NavMesh.SamplePosition(
                randomTarget,
                out NavMeshHit targetHit,
                config.WanderRadius + 10f,
                NavMesh.AllAreas
            );

            if (!startOnMesh)
            {
                logger.Log(
                    $"[AINav] {gameObject.name} at {rootPos} is too far from any NavMesh.",
                    this,
                    Logging.LogType.Error
                );
                TransitionTo(AIState.Idle);
                return;
            }

            if (
                !targetOnMesh
                || !NavMesh.CalculatePath(
                    startHit.position,
                    targetHit.position,
                    NavMesh.AllAreas,
                    currentPath
                )
            )
            {
                logger.Log(
                    $"[AINav] {gameObject.name} could not find a valid path. Staying idle.",
                    this,
                    Logging.LogType.Warning
                );
                TransitionTo(AIState.Idle);
                return;
            }

            if (currentPath.status != NavMeshPathStatus.PathComplete)
            {
                logger.Log(
                    $"[AINav] {gameObject.name} path incomplete ({currentPath.status}). Staying idle.",
                    this,
                    Logging.LogType.Warning
                );
                TransitionTo(AIState.Idle);
                return;
            }

            currentPathIndex = 0;
            TransitionTo(AIState.Wandering);
            logger.Log(
                $"[AINav] {gameObject.name} began wandering ({currentPath.corners.Length} waypoints).",
                this
            );
        }

        private void ProcessMovementAlongPath()
        {
            if (currentPath == null || currentPathIndex >= currentPath.corners.Length)
            {
                TransitionTo(AIState.Idle);
                return;
            }

            Vector3 rootPos = Flat(transform.root.position);
            Vector3 waypoint = Flat(currentPath.corners[currentPathIndex]);

            if (Vector3.Distance(rootPos, waypoint) <= config.StoppingDistance)
            {
                currentPathIndex++;

                // Reached the final waypoint — go idle
                if (currentPathIndex >= currentPath.corners.Length)
                {
                    TransitionTo(AIState.Idle);
                    return;
                }

                waypoint = Flat(currentPath.corners[currentPathIndex]);
            }

            Vector3 direction = (waypoint - rootPos).normalized;

            // Throttle MoveCommand sends — only dispatch when direction meaningfully changes
            if (Vector3.Distance(direction, lastSentDirection) > 0.05f)
                SendMove(direction);
        }

        private void SendMove(Vector3 direction)
        {
            lastSentDirection = direction;
            worldMonitor.Commands.Enqueue(new MoveCommand(netId, direction));
        }

        private void OnDrawGizmos()
        {
            Vector3 center = Application.isPlaying ? spawnCenter : transform.position;

            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(center, config.WanderRadius);

            if (!Application.isPlaying || currentPath == null || currentPath.corners.Length == 0)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(currentPath.corners[i], currentPath.corners[i + 1]);
                Gizmos.DrawSphere(currentPath.corners[i], 0.1f);
            }
            Gizmos.DrawSphere(currentPath.corners[^1], 0.2f);
        }
    }
}
