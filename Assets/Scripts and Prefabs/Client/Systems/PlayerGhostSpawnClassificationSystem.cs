using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

//We are updating only in the client world because only the client must specify exactly which player entity it "owns"
[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
//We will be updating after NetCode's GhostSpawnClassificationSystem because we want
//to ensure that the PredictedGhostComponent (which it adds) is available on the player entity to identify it
[UpdateInGroup(typeof(GhostSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSpawnClassificationSystem))]
public class PlayerGhostSpawnClassificationSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    //We will store the Camera prefab here which we will attach when we identify our player entity
    private Entity m_CameraPrefab;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();

        //We need to make sure we have NCE before we start the update loop (otherwise it's unnecessary)
        RequireSingletonForUpdate<NetworkIdComponent>();
    }

    struct GhostPlayerState : ISystemStateComponentData
    {
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
        var playerEntity = GetSingletonEntity<NetworkIdComponent>();
        //The false is to signify that the data will NOT be read-only
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>(false);

        //We search for player entities with a PredictedGhostComponent (which means it is ours)
        Entities
        .WithAll<PlayerTag, PredictedGhostComponent>()
        .WithNone<GhostPlayerState>()
        .WithNativeDisableParallelForRestriction(commandTargetFromEntity)
        .ForEach((Entity entity, int entityInQueryIndex) =>
        {
            //Here is where we update the NCE's CommandTargetComponent targetEntity to point at our player entity
            var state = commandTargetFromEntity[playerEntity];
            state.targetEntity = entity;
            commandTargetFromEntity[playerEntity] = state;
            
            //Here we add a special "ISystemStateComponentData" component
            //This component does NOT get deleted with the rest of the entity
            //Unity provided this special type of component to provide us a way to do "clean up" when important entities are destroyed
            //In our case we will use GhostPlayerState to know when our player entity has been destroyed and will
            //allow us to set our NCE CommandTargetComponent's targetEntity field back to null
            //It also helps us ensure that we only set up our player entity once because we have a 
            //.WithNone<GhostPlayerState>() on our entity query
            commandBuffer.AddComponent(entityInQueryIndex, entity, new GhostPlayerState());

            //This creates our camera
            var cameraEntity = commandBuffer.Instantiate(entityInQueryIndex, camera);
            //This is how you "attach" a prefab entity to another
            commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new Parent { Value = entity });
            commandBuffer.AddComponent(entityInQueryIndex, cameraEntity, new LocalToParent() );

        }).ScheduleParallel();

        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}
