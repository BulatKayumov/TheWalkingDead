using UnityEngine;
using Unity.Entities;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter(typeof(NavAgentSystem))]
    public class WaitingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithName("DetectNotWaitingIdleAgentJob")
                .ForEach((ref WaitingEntity waitingEntity,
                in NavAgent agent) =>
                {
                    if(agent.status == AgentStatus.Idle && !waitingEntity.ready && !waitingEntity.isWaiting)
                    {
                        waitingEntity.isWaiting = true;
                        var random = new Unity.Mathematics.Random((uint)((agent.position.x + agent.position.z) * agent.moveSpeed));
                        waitingEntity.waitingTime = random.NextFloat(5, 15);
                    }
                }).Schedule();

            var dt = Time.DeltaTime;

            Entities
                .WithName("WaitingJob")
                .ForEach((ref WaitingEntity waitingEntity,
                in NavAgent agent) =>
                {
                    if (waitingEntity.isWaiting)
                    {
                        if(waitingEntity.waitingTime > 0)
                        {
                            waitingEntity.waitingTime -= dt;
                        }
                        else
                        {
                            waitingEntity.isWaiting = false;
                            waitingEntity.ready = true;
                        }
                    }
                }).Schedule();
        }
    }
}