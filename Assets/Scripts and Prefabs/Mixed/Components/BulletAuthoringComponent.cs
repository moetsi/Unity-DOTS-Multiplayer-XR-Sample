using Unity.Entities;

[GenerateAuthoringComponent]
public struct BulletAuthoringComponent : IComponentData
{
    public Entity Prefab;
}