using System;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct WaitingEntity : IComponentData {
    public float waitingTime;
    public bool isWaiting;
    public bool ready;
}
