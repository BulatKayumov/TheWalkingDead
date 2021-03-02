using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using TWD_Components;

namespace TWD_Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    [UpdateAfter(typeof(NavAgentSystem))]
    public class PlayerControlSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = Time.DeltaTime;
            Entities
                .WithoutBurst()
                .ForEach((ref Translation translation,
                ref Rotation rotation,
                in Player settings) =>
                {
                    Quaternion rot = new Quaternion(rotation.Value.value.x, rotation.Value.value.y, rotation.Value.value.z, rotation.Value.value.w);
                    rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y + Input.GetAxis("Horizontal") * settings.angleSpeed * dt, rot.eulerAngles.z);
                    rotation.Value = new quaternion(rot.x, rot.y, rot.z, rot.w);
                    translation.Value = translation.Value + (dt * Input.GetAxis("Vertical") * settings.speed * math.forward(rotation.Value));
                }).Run();
        }
    }
}