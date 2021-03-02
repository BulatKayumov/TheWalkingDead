using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TWD_Components
{
    [GenerateAuthoringComponent]
    public struct NavAgentAvoidance : IComponentData
    {
        public float radius;
    }
}