using Unity.Entities;
using Unity.NetCode;

[GenerateAuthoringComponent]
public struct PlayerEntityComponent : IComponentData
{
    public Entity PlayerEntity;
}