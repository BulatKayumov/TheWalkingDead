using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;
using TWD_Components;

namespace TWD_Systems
{
    public struct QuadrantData
    {
        public Entity entity;
        public float3 position;
    }
    public class QuadrantSystem : ComponentSystem
    {
        public const int quadrantMultiplier = 40000;
        private const int quadrantCellSize = 200;

        public static NativeMultiHashMap<int, QuadrantData> agentsQuadrantMultiHashMap;
        public static NativeMultiHashMap<int, QuadrantData> targetsQuadrantMultiHashMap;

        EntityQuery agentsEntityQuery;
        EntityQuery targetsEntityQuery;

        protected override void OnCreate()
        {
            agentsEntityQuery = GetEntityQuery(typeof(NavAgent), typeof(QuadrantEntity));
            targetsEntityQuery = GetEntityQuery(typeof(Target), typeof(QuadrantEntity));
            agentsQuadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
            targetsQuadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            agentsQuadrantMultiHashMap.Dispose();
            targetsQuadrantMultiHashMap.Dispose();
        }

        public static int GetPositionHashMapKey(float3 position)
        {
            return (int)(math.floor(position.x / quadrantCellSize) + quadrantMultiplier * math.floor(position.z / quadrantCellSize));
        }

        protected override void OnUpdate()
        {
            agentsQuadrantMultiHashMap.Clear();
            targetsQuadrantMultiHashMap.Clear();
            var agentsEntityCount = agentsEntityQuery.CalculateEntityCount();
            if (agentsEntityCount > agentsQuadrantMultiHashMap.Capacity)
            {
                agentsQuadrantMultiHashMap.Capacity = agentsEntityCount;
            }
            var targetEntityCount = targetsEntityQuery.CalculateEntityCount();
            if (targetEntityCount > targetsQuadrantMultiHashMap.Capacity)
            {
                targetsQuadrantMultiHashMap.Capacity = targetEntityCount;
            }

            Entities
                .WithAll<Zombie, QuadrantEntity>()
                .ForEach((Entity entity,
                ref NavAgent agent) =>
                {
                    int hashMapKey = GetPositionHashMapKey(agent.position);
                    agentsQuadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                    {
                        entity = entity,
                        position = agent.position
                    });
                });

            Entities
                .WithAll<Target, QuadrantEntity>()
                .ForEach((Entity entity,
                ref Translation translation) =>
                {
                    int hashMapKey = GetPositionHashMapKey(translation.Value);
                    targetsQuadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                    {
                        entity = entity,
                        position = translation.Value
                    });
                });
        }
    }
}