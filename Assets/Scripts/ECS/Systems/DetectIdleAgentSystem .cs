using UnityEngine;
using UnityEngine.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using TWD_Components;
using TWD_Scripts;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter(typeof(WaitingSystem))]
    public class DetectIdleAgentSystem : JobComponentSystem
    {

        NativeArray<NavAgent> agents;
        NativeArray<Entity> entities;
        private NativeQueue<AgentData> needsPath = new NativeQueue<AgentData>(Allocator.Persistent);

        public struct AgentData
        {
            public Entity entity;
            public NavAgent agent;
        }

        private struct SetNextPathJob : IJob
        {
            public NativeQueue<AgentData> needsPath;
            public int randomSeed;
            public void Execute()
            {
                while (needsPath.TryDequeue(out AgentData item))
                {
                    item.agent.needsPath = false;
                    var position = item.agent.position;
                    var randomSeedModifier = (int)(item.agent.position.x + item.agent.position.z) / item.entity.Index;
                    var random = new Unity.Mathematics.Random((uint)(randomSeed + randomSeedModifier));
                    var destination = new float3(position.x + random.NextFloat(-500, 500), 8, position.z + random.NextFloat(-500, 500));
                    NavAgentSystem.SetDestinationStatic(item.entity, item.agent, destination, item.agent.areaMask);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var randomSeed = UnityEngine.Random.Range(1, 999999);
            var needsPathParallelWriter = needsPath.AsParallelWriter();
            inputDeps = Entities
                .WithAll<Zombie>()
                .WithName("DetectIdleAgentJob")
                .ForEach((ref NavAgent agent,
                in WaitingEntity waitingEntity,
                in Entity entity) =>
                {
                    if (agent.status == AgentStatus.Idle && waitingEntity.ready)
                    {
                        agent.needsPath = true;
                        needsPathParallelWriter.Enqueue(new AgentData { entity = entity, agent = agent });
                        agent.status = AgentStatus.PathQueued;
                    }
                }).Schedule(inputDeps);

            inputDeps = new SetNextPathJob
            {
                needsPath = needsPath,
                randomSeed = randomSeed
            }.Schedule(inputDeps);

            return inputDeps;
        }
    }
}