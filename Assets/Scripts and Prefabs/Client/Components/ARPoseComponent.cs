using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

public struct ARPoseComponent : IComponentData
{
    public Translation translation;
    public Rotation rotation;
}