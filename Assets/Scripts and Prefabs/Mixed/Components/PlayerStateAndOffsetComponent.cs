using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct PlayerStateAndOffsetComponent : IComponentData
{
    public float3 Value;
    [GhostField]
    public int State;
    public uint WeaponCooldown;

}