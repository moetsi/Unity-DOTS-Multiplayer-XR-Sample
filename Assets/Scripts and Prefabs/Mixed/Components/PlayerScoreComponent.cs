using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct PlayerScoreComponent : IComponentData
{
    [GhostField]
    public int networkId;
    [GhostField]
    public FixedString64Bytes playerName;
    [GhostField]
    public int currentScore;
    [GhostField]
    public int highScore;
}