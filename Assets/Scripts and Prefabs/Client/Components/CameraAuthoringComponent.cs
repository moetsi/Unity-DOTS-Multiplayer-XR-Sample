using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct CameraAuthoringComponent : IComponentData
{
    public Entity Prefab;
}
