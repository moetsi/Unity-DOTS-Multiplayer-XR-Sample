using Unity.Entities;
using UnityEngine;

public class PlayerCommandBufferAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<PlayerCommand>(entity);
    }
}