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
    [UpdateAfter(typeof(QuadrantSystem))]
    public class TargetSearchSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var quadrantMultiHashMap = QuadrantSystem.targetsQuadrantMultiHashMap;
            var dt = Time.DeltaTime;
            Entities
                .WithBurst()
                .WithName("TargetSearchJob")
                .WithReadOnly(quadrantMultiHashMap)
                .ForEach((ref Zombie zombie,
                ref NavAgent agent,
                in Entity entity) =>
                {
                    if(zombie.timer > 0)
                    {
                        zombie.timer -= dt;
                    }
                    else
                    {
                        var hashMapKey = QuadrantSystem.GetPositionHashMapKey(agent.position);
                        int iterator = 0;
                        var hashMapKeys = new NativeArray<int>(41, Allocator.Temp);
                        for (int i = -4; i < 5; i++)
                        {
                            for (int j = math.abs(i) - 4; j < 5 - math.abs(i); j++)
                            {
                                hashMapKeys[iterator] = hashMapKey + i + QuadrantSystem.quadrantMultiplier * j;
                                iterator++;
                            }
                        }

                        QuadrantData quadrantData;
                        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
                        float3 closestTargetPosition = new float3(0, 0, 0);
                        float closestDistance = 99999;
                        for (int i = 0; i < hashMapKeys.Length; i++)
                        {
                            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
                            {
                                do
                                {
                                    var distance = math.distance(agent.position, quadrantData.position);
                                    if (distance < closestDistance)
                                    {
                                        zombie.targetPosition = quadrantData.position;
                                        closestDistance = distance;
                                        zombie.targetFound = true;
                                    }
                                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
                            }
                        }
                    }
                }).Schedule();

            Entities
                .WithoutBurst()
                .WithName("ChaseTargetJob")
                .ForEach((ref Zombie zombie,
                ref NavAgent agent,
                in Entity entity) =>
                {
                    if (zombie.targetFound)
                    {
                        zombie.targetFound = false;
                        zombie.isChasingTarget = true;
                        zombie.timer = 1;
                        agent.status = AgentStatus.PathQueued;
                        NavAgentSystem.SetDestinationStatic(entity, agent, zombie.targetPosition, agent.areaMask);
                    }
                }).Schedule();
        }
    }
}