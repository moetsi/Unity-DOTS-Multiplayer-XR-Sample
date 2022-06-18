using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct HighestScoreComponent : IComponentData
{
    [GhostField]
    public FixedString64Bytes playerName;

    [GhostField]
    public int highestScore;
}