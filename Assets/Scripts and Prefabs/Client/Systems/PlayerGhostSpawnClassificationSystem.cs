using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

//We are updating only in the client world because only the client must specify exactly which player entity it "owns"
[UpdateInWorld(TargetWorld.Client)]
//We will be updating after NetCode's GhostSpawnClassificationSystem because we want
//to ensure that the PredictedGhostComponent (which it adds) is available on the player entity to identify it
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSpawnClassificationSystem))]
public partial class PlayerGhostSpawnClassificationSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    //We will store the Camera prefab here which we will attach when we identify our player entity
    private Entity m_CameraPrefab;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();

        //We need to make sure we have NCE before we start the update loop (otherwise it's unnecessary)
        RequireSingletonForUpdate<NetworkIdComponent>();
        RequireSingletonForUpdate<CameraAuthoringComponent>();
    }

    protected override void OnUpdate()
    {
        //Here we set the prefab we will use
        if (m_CameraPrefab == Entity.Null)
        {
            //We grab our camera and set our variable
            m_CameraPrefab = GetSingleton<CameraAuthoringComponent>().Prefab;
            return;
        }
        
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer().AsParallelWriter();
        
        //We must declare our local variables before using them
        var camera = m_CameraPrefab;
        //The "playerEntity" is the NCE
        var networkIdComponent = GetSingleton<NetworkIdComponent>();
        //The false is to signify that the data will NOT be read-only
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>(false);

        //We will look for Player prefabs that we have not added a "PlayerClassifiedTag" to (which means we have checked the player if it is "ours")
        Entities
        .WithAll<PlayerTag>()
        .WithNone<PlayerClassifiedTag>()
        .ForEach((Entity entity, int entityInQueryIndex, in GhostOwnerComponent ghostOwnerComponent) =>
        {
            // If this is true this means this Player is mine (because the GhostOwnerComponent value is equal to the NetworkId)
            // Remember the GhostOwnerComponent value is set by the server and is ghosted to the client
            if (ghostOwnerComponent.NetworkId == networkIdComponent.Value)
            {
                //This creates our camera
                var cameraEntity = commandBuffer.Instantiate(entityInQueryIndex, camera);
                //This is how you "attach" a prefab entity to another
                commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new Parent { Value = entity });
                commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new LocalToParent() );
            }
            // This means we have classified this Player prefab
            commandBuffer.AddComponent(entityInQueryIndex, entity, new PlayerClassifiedTag() );

        }).ScheduleParallel();

        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}
