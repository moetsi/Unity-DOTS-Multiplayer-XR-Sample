using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct HighestScoreComponent : IComponentData
{
    [GhostField]
    public FixedString64 playerName;

    [GhostField]
    public int highestScore;
}