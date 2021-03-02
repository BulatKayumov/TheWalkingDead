using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateBefore (typeof (QuadrantSystem))]
    public class NavAgentFromPositionSyncSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            inputDeps = Entities
                .WithAll<SyncPositionToNavAgent>()
                .ForEach((ref NavAgent navAgent,
                in Translation translation) =>
                {
                    navAgent.position = translation.Value;
                }).Schedule(inputDeps);
            return inputDeps;
        }
    }

    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter (typeof (NavAgentSystem))]
    public class NavAgentToPositionSyncSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            inputDeps = Entities
                .WithAll<SyncPositionFromNavAgent>()
                .ForEach((ref Translation translation,
                in NavAgent navAgent) =>
                {
                    translation.Value = navAgent.position;
                }).Schedule(inputDeps);
            return inputDeps;
        }
    }

    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateBefore (typeof (QuadrantSystem))]
    public class NavAgentFromRotationSyncSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            inputDeps = Entities
                .WithAll<SyncRotationToNavAgent>()
                .ForEach((ref NavAgent navAgent,
                in Rotation rotation) =>
                {
                    navAgent.rotation = rotation.Value;
                }).Schedule(inputDeps);
            return inputDeps;
        }
    }

    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter (typeof (NavAgentSystem))]
    public class NavAgentToRotationSyncSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {
            inputDeps = Entities
                .WithAll<SyncRotationFromNavAgent>()
                .ForEach((ref Rotation rotation,
                in NavAgent navAgent) =>
                {
                    rotation.Value = navAgent.rotation;
                }).Schedule(inputDeps);
            return inputDeps;
        }
    }
}