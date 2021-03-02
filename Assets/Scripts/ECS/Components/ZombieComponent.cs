using Unity.Mathematics;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct Zombie : IComponentData {
    public bool isChasingTarget;
    public bool targetFound;
    public float3 targetPosition;
    public bool isFollowingZombie;
    public float timer;
}
