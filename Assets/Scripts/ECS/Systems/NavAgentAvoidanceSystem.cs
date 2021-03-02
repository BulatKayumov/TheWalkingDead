using UnityEngine;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateBefore(typeof(NavAgentSystem))]
    public class NavAgentAvoidanceSystem : JobComponentSystem
    {
        NavMeshQuery navMeshQuery;

        protected override void OnCreate()
        {
            navMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 128);
        }

        protected override void OnDestroy()
        {
            navMeshQuery.Dispose();
        }

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            var navMeshQuery2 = navMeshQuery;
            var dt = Time.DeltaTime;
            var quadrantMultiHashMap = QuadrantSystem.agentsQuadrantMultiHashMap;
            var avoidanceJob = Entities
                .WithBurst()
                .WithAll<Zombie>()
                .WithName("AvoidanceJob")
                .WithReadOnly(quadrantMultiHashMap)
                .WithReadOnly(navMeshQuery2)
                .ForEach((ref NavAgent agent,
                ref NavAgentAvoidance avoidance,
                in Entity entity) =>
                {
                    if(agent.status == AgentStatus.Moving)
                    {
                        var hashMapKey = QuadrantSystem.GetPositionHashMapKey(agent.position);

                        var hashMapKeys = new NativeArray<int>(9, Allocator.Temp);
                        hashMapKeys[0] = hashMapKey;
                        hashMapKeys[1] = hashMapKey + 1;
                        hashMapKeys[2] = hashMapKey - 1;
                        hashMapKeys[3] = hashMapKey + QuadrantSystem.quadrantMultiplier;
                        hashMapKeys[4] = hashMapKey - QuadrantSystem.quadrantMultiplier;
                        hashMapKeys[5] = hashMapKey + 1 + QuadrantSystem.quadrantMultiplier;
                        hashMapKeys[6] = hashMapKey + 1 - QuadrantSystem.quadrantMultiplier;
                        hashMapKeys[7] = hashMapKey - 1 + QuadrantSystem.quadrantMultiplier;
                        hashMapKeys[8] = hashMapKey - 1 - QuadrantSystem.quadrantMultiplier;

                        QuadrantData quadrantData;
                        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;

                        for (int i = 0; i < hashMapKeys.Length; i++)
                        {
                            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
                            {
                                do
                                {
                                    if (quadrantData.entity.Index != entity.Index)
                                    {
                                        if (math.distance(agent.position, quadrantData.position) < avoidance.radius * 2)
                                        {
                                            var forwardVector = (agent.nextPosition - agent.position);
                                            var otherAgentDirection = (quadrantData.position - agent.position);
                                            if (forwardVector.x * otherAgentDirection.x + forwardVector.z * otherAgentDirection.z > 0)
                                            {
                                                var move = Vector3.right;
                                                if (forwardVector.x * otherAgentDirection.z - forwardVector.z * otherAgentDirection.x < 0)
                                                {
                                                    move = Vector3.left;
                                                }
                                                float3 drift = Quaternion.LookRotation(otherAgentDirection) * (/*Vector3.forward + */move) * agent.currentMoveSpeed * dt;
                                                if (agent.nextWaypointIndex != agent.totalWaypoints)
                                                {
                                                    var offsetWaypoint = agent.currentWaypoint + drift;
                                                    var waypointInfo = navMeshQuery2.MapLocation(offsetWaypoint, Vector3.one * 3f, 0, agent.areaMask);
                                                    if (navMeshQuery2.IsValid(waypointInfo))
                                                    {
                                                        agent.currentWaypoint = waypointInfo.position;
                                                    }
                                                }
                                                agent.currentMoveSpeed = math.min(agent.currentMoveSpeed, agent.moveSpeed / 2f);
                                                var positionInfo = navMeshQuery2.MapLocation(agent.position + drift, Vector3.one * 3f, 0, agent.areaMask);
                                                if (navMeshQuery2.IsValid(positionInfo))
                                                {
                                                    agent.nextPosition = positionInfo.position;
                                                }
                                                else
                                                {
                                                    agent.nextPosition = agent.position;
                                                }
                                            }
                                        }
                                    }
                                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
                            }
                        }
                        hashMapKeys.Dispose();
                    }
                }).Schedule(inputDeps);
            return avoidanceJob;
        }
    }

}