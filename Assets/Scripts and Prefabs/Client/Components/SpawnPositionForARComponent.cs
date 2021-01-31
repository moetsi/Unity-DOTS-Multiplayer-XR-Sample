using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public struct SpawnPositionForARComponent : IComponentData
{
    public float3 spawnTranslation;
    public quaternion spawnRotation;
}