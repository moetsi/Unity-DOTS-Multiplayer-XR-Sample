using Unity.Entities;

[GenerateAuthoringComponent]
public struct PlayerScoreAuthoringComponent : IComponentData
{
    public Entity Prefab;
}