using Unity.Entities;

[GenerateAuthoringComponent]
public struct PlayerAuthoringComponent : IComponentData
{
    public Entity Prefab;
}