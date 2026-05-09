using System.Collections;
using System.Collections.Generic;
using FTR.Core.Server;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class AINavigationSystem : MonoBehaviour
    {
        private const float CombatRangePadding = 0.75f;

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
        private PlayerTriggerArea _chaseTriggerArea;
        private List<Collider> activeTargets = new List<Collider>();
        private Collider currentTarget;
        private float chaseStopDistance;

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

            stateStorage.OnEquippedItemChanged += HandleEquippedItemChanged;
            HandleEquippedItemChanged(stateStorage.EquippedItemId);

            gameTickEvent.OnRaised += GameTick;

            isInitialized = true;

            TransitionTo(AIState.Idle);
        }

        private void OnDisable()
        {
            gameTickEvent.OnRaised -= GameTick;
        }

        private void OnDestroy()
        {
            if (stateStorage != null)
                stateStorage.OnEquippedItemChanged -= HandleEquippedItemChanged;

            if (_chaseTriggerArea != null)
            {
                _chaseTriggerArea.OnPlayerEnter -= OnChaseStart;
                _chaseTriggerArea.OnPlayerExit -= OnChaseStop;
            }
        }

        public void SetChaseTriggerArea(PlayerTriggerArea chaseTriggerArea)
        {
            _chaseTriggerArea = chaseTriggerArea;

            if (_chaseTriggerArea != null)
            {
                _chaseTriggerArea.OnPlayerEnter += OnChaseStart;
                _chaseTriggerArea.OnPlayerExit += OnChaseStop;

                UpdateChaseTriggerAreaFromEquipment(stateStorage?.EquippedItemId);
            }
        }

        private void HandleEquippedItemChanged(string equippedItemId) =>
            UpdateChaseTriggerAreaFromEquipment(equippedItemId);

        private void UpdateChaseTriggerAreaFromEquipment(string equippedItemId)
        {
            float attackRange = ResolveAttackRange(equippedItemId);
            chaseStopDistance = Mathf.Max(0.25f, attackRange - CombatRangePadding);

            if (_chaseTriggerArea != null)
            {
                float chaseRadius = Mathf.Max(
                    config.AggressiveChaseRadius,
                    attackRange + CombatRangePadding
                );
                _chaseTriggerArea.Initialize(chaseRadius);
            }
        }

        private float ResolveAttackRange(string equippedItemId)
        {
            if (string.IsNullOrEmpty(equippedItemId))
                return config.UnequippedRange;

            WeaponItemData weapon = ServerItemsRegistry.GetWeaponById(equippedItemId);
            if (weapon != null)
                return weapon.range;

            return config.UnequippedRange;
        }

        private void GameTick(float dt)
        {
            // TODO(optimization): revise this approach, can we save ticks?
            // e.g. no players? no movement
            if (!isInitialized)
                return;

            if (stateStorage.IsMovementBlocked)
            {
                if (currentState != AIState.Idle)
                {
                    TransitionTo(AIState.Idle);
                }
                return;
            }

            if (currentState == AIState.Wandering || currentState == AIState.Chasing)
            {
                ProcessMovementAlongPath();
            }
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

                case AIState.Chasing:
                    StopAllCoroutines();
                    StartCoroutine(ChaseRoutine());
                    break;
            }
        }

        // Waits a random duration then tries to pick a new wander target.
        private IEnumerator IdleRoutine()
        {
            yield return new WaitForSeconds(Random.Range(config.MinWaitTime, config.MaxWaitTime));
            TryBeginWander();
        }

        private IEnumerator ChaseRoutine()
        {
            while (currentState == AIState.Chasing && currentTarget != null)
            {
                UpdateChasePath();
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void UpdateChasePath()
        {
            if (currentTarget == null || !stateStorage.IsGrounded)
                return;

            Vector3 rootPos = transform.root.position;
            Vector3 targetPos = currentTarget.transform.position;

            bool startOnMesh = NavMesh.SamplePosition(
                rootPos,
                out NavMeshHit startHit,
                20f,
                NavMesh.AllAreas
            );
            bool targetOnMesh = NavMesh.SamplePosition(
                targetPos,
                out NavMeshHit targetHit,
                20f,
                NavMesh.AllAreas
            );

            if (startOnMesh && targetOnMesh)
            {
                NavMesh.CalculatePath(
                    startHit.position,
                    targetHit.position,
                    NavMesh.AllAreas,
                    currentPath
                );
                currentPathIndex = 0;
            }
        }

        public void OnChaseStart(Collider target)
        {
            if (!activeTargets.Contains(target))
                activeTargets.Add(target);

            currentTarget = target;

            if (currentState != AIState.Chasing)
            {
                TransitionTo(AIState.Chasing);
            }
        }

        public void OnChaseStop(Collider target)
        {
            activeTargets.Remove(target);

            if (currentTarget == target)
            {
                if (activeTargets.Count > 0)
                {
                    currentTarget = activeTargets[activeTargets.Count - 1];
                }
                else
                {
                    currentTarget = null;
                    TransitionTo(AIState.Idle);
                }
            }
        }

        private void TryBeginWander()
        {
            if (stateStorage.IsMovementBlocked)
            {
                TransitionTo(AIState.Idle);
                return;
            }

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
                TransitionTo(AIState.Idle);
                return;
            }

            if (currentPath.status != NavMeshPathStatus.PathComplete)
            {
                TransitionTo(AIState.Idle);
                return;
            }

            currentPathIndex = 0;
            TransitionTo(AIState.Wandering);
        }

        private void ProcessMovementAlongPath()
        {
            Vector3 rootPos;
            if (currentState == AIState.Chasing && currentTarget != null)
            {
                rootPos = Flat(transform.root.position);
                Vector3 targetPos = Flat(currentTarget.transform.position);

                if (Vector3.Distance(rootPos, targetPos) <= chaseStopDistance)
                {
                    SendMove(Vector3.zero);
                    return;
                }
            }

            if (currentPath == null || currentPathIndex >= currentPath.corners.Length)
            {
                if (currentState == AIState.Wandering)
                    TransitionTo(AIState.Idle);
                else if (currentState == AIState.Chasing)
                    SendMove(Vector3.zero);
                return;
            }

            rootPos = Flat(transform.root.position);
            Vector3 waypoint = Flat(currentPath.corners[currentPathIndex]);

            float stoppingDistance =
                currentState == AIState.Chasing ? chaseStopDistance : config.StoppingDistance;

            if (Vector3.Distance(rootPos, waypoint) <= stoppingDistance)
            {
                currentPathIndex++;

                // Reached the final waypoint — go idle
                if (currentPathIndex >= currentPath.corners.Length)
                {
                    if (currentState == AIState.Wandering)
                        TransitionTo(AIState.Idle);
                    else if (currentState == AIState.Chasing)
                        SendMove(Vector3.zero);
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
