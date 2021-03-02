using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerControlSystem))]
    public class CamFollowSystemSystem : SystemBase
    {
        private EntityQuery playerQuery;
        private Camera mainCamera;
        protected override void OnCreate()
        {
            playerQuery = GetEntityQuery(ComponentType.ReadOnly<Player>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>());
        }

        protected override void OnStartRunning()
        {
            mainCamera = Camera.main;   
        }

        protected override void OnUpdate()
        {
            var playerPosition = playerQuery.ToComponentDataArray<Translation>(Allocator.Temp);
            var playerRotation = playerQuery.ToComponentDataArray<Rotation>(Allocator.Temp);
            if(playerPosition.Length != 0)
            {
                mainCamera.transform.position = playerPosition[0].Value + math.forward(playerRotation[0].Value) * -500 + new float3(0, 500, 0);
                mainCamera.transform.LookAt(playerPosition[0].Value + new float3(0, 300, 0));
            }
        }
    }
}