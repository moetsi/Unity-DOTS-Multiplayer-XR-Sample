using Unity.Entities;

[GenerateAuthoringComponent]
public struct AsteroidAuthoringComponent : IComponentData
{
    public Entity Prefab;
}