using System;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct Player : IComponentData
{
    public float speed;
    public float angleSpeed;
}
