using Unity.Entities;
using Unity.Mathematics;

namespace TWD_builder
{
    public struct BuildingData : IComponentData
    {
        public Entity Entity;
        public float3 Position;
        public BuildingType Type;
    }
}