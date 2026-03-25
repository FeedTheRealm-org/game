using System.Collections;
using FTR.Core.Server;
using FTR.Core.Server.Commands;
using UnityEngine;
using UnityEngine.AI;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class AINavigationSystem : MonoBehaviour
    {
        [Header("Wander Settings")]
        [SerializeField]
        private float wanderRadius = 10f;

        [SerializeField]
        private float minWaitTime = 2f;

        [SerializeField]
        private float maxWaitTime = 5f;

        [SerializeField]
        private float stoppingDistance = 0.5f;

        [Header("Debug")]
        [SerializeField]
        private bool showGizmos = true;

        private WorldMonitor worldMonitor;
        private uint netId;
        private Vector3 spawnCenter;
        private bool isInitialized;
        private NavMeshPath currentPath;
        private int currentPathIndex;
        private Vector3 lastSentDirection = Vector3.zero;

        private enum AIState
        {
            Idle,
            Wandering,
        }

        private AIState currentState = AIState.Idle;

        /// <summary>
        /// Initializes the AI Navigation system.
        /// Call this from your linker or spawner when setting up the NPC/Enemy entity.
        /// </summary>
        public void Initialize(
            uint netId,
            WorldMonitor worldMonitor,
            Vector3? spawnCenter = null,
            float? customRadius = null
        )
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
            this.spawnCenter = spawnCenter ?? transform.position;

            if (customRadius.HasValue)
                this.wanderRadius = customRadius.Value;

            currentPath = new NavMeshPath();
            isInitialized = true;

            StartCoroutine(WanderRoutine());
        }

        private IEnumerator WanderRoutine()
        {
            while (true)
            {
                if (!isInitialized)
                {
                    yield return null;
                    continue;
                }

                if (currentState == AIState.Idle)
                {
                    float waitTime = Random.Range(minWaitTime, maxWaitTime);
                    yield return new WaitForSeconds(waitTime);
                    SetNewWanderTarget();
                }
                else if (currentState == AIState.Wandering)
                {
                    ProcessMovement();
                }

                yield return null; // Update the movement processing every frame
            }
        }

        private void SetNewWanderTarget()
        {
            // Abort if ground check fails to ensure NavMesh isn't calculating mid-air
            var stateStorage =
                transform.GetComponentInParent<FTR.Gameplay.Common.NetworkEntities.Characters.CharacterStateStorage>();
            if (stateStorage != null && !stateStorage.IsGrounded)
            {
                Debug.LogWarning(
                    $"[AINav] {gameObject.name} is not grounded yet, deferring NavMesh pathing."
                );
                currentState = AIState.Idle;
                return;
            }

            // Use transform.root.position to ensure we are grabbing the coordinates of the base NPC GameObject,
            // not the newly added child `serverComponents` wrapper which might be offset
            Vector3 startPos = transform.root.position;

            // Pick a random direction within the defined wander radius
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnCenter;

            // Increase search radius significantly. If it's a dynamic NavMesh, the exact Y could vary,
            // and the built mesh bounding box generation might slightly float above physical colliders.
            if (NavMesh.SamplePosition(startPos, out NavMeshHit startHit, 20.0f, NavMesh.AllAreas))
            {
                // Sample the NavMesh to find the closest valid target point
                if (
                    NavMesh.SamplePosition(
                        randomDirection,
                        out NavMeshHit targetHit,
                        wanderRadius + 10f,
                        NavMesh.AllAreas
                    )
                )
                {
                    if (
                        NavMesh.CalculatePath(
                            startHit.position,
                            targetHit.position,
                            NavMesh.AllAreas,
                            currentPath
                        )
                    )
                    {
                        if (currentPath.status == NavMeshPathStatus.PathComplete)
                        {
                            currentPathIndex = 0;
                            currentState = AIState.Wandering;
                            Debug.Log($"[AINav] {gameObject.name} found path. Wandering!");
                            return;
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[AINav] {gameObject.name} path status was {currentPath.status}. Idle."
                            );
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[AINav] {gameObject.name} failed to calculate NavMesh path."
                        );
                    }
                }
            }
            else
            {
                Debug.LogError(
                    $"[AINav] {gameObject.name}'s transform ({startPos}) is too far from a NavMesh! Can't start pathing."
                );
            }

            // If failed to find a valid path, remain idle and try again next loop
            currentState = AIState.Idle;
        }

        private void ProcessMovement()
        {
            if (
                currentPath == null
                || currentPath.corners.Length == 0
                || currentPathIndex >= currentPath.corners.Length
            )
            {
                StopMoving();
                return;
            }

            Vector3 targetPosition = currentPath.corners[currentPathIndex];

            // Calculate distance ignoring the Y axis
            Vector3 currentPosFlat = new Vector3(
                transform.root.position.x,
                0,
                transform.root.position.z
            );
            Vector3 targetPosFlat = new Vector3(targetPosition.x, 0, targetPosition.z);

            float distance = Vector3.Distance(currentPosFlat, targetPosFlat);

            if (distance <= stoppingDistance)
            {
                currentPathIndex++;
                if (currentPathIndex >= currentPath.corners.Length)
                {
                    StopMoving();
                    return;
                }

                targetPosition = currentPath.corners[currentPathIndex];
                targetPosFlat = new Vector3(targetPosition.x, 0, targetPosition.z);
            }

            // Move towards the target waypoint
            Vector3 direction = (targetPosFlat - currentPosFlat).normalized;

            // Only send MoveCommand if the direction changed to prevent flooding the server queues (0.01 threshold)
            if (Vector3.Distance(direction, lastSentDirection) > 0.05f)
            {
                lastSentDirection = direction;
                var moveCommand = new MoveCommand(netId, direction);
                worldMonitor.Commands.Enqueue(moveCommand);
            }
        }

        private void StopMoving()
        {
            currentState = AIState.Idle;
            currentPath = new NavMeshPath(); // Clear the path

            // Send a final zero direction to bring the character to a halt
            if (lastSentDirection != Vector3.zero)
            {
                lastSentDirection = Vector3.zero;
                worldMonitor.Commands.Enqueue(new MoveCommand(netId, Vector3.zero));
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            // Draw wander radius constraint radially around the configured spawn center
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Vector3 center = Application.isPlaying ? spawnCenter : transform.position;
            Gizmos.DrawWireSphere(center, wanderRadius);

            // Draw current path waypoints and lines
            if (Application.isPlaying && currentPath != null && currentPath.corners.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < currentPath.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(currentPath.corners[i], currentPath.corners[i + 1]);
                    Gizmos.DrawSphere(currentPath.corners[i], 0.1f);
                }
                Gizmos.DrawSphere(currentPath.corners[currentPath.corners.Length - 1], 0.2f);
            }
        }
    }
}
