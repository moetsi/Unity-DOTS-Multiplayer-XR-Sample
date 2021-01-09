using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GenerateAuthoringComponent]
public struct VelocityComponent : IComponentData
{
    [GhostField]
    public float3 Linear;
}