using Unity.Entities;
using Unity.NetCode;

public struct PlayerSpawningStateComponent : IComponentData
{
    public int IsSpawning;
}