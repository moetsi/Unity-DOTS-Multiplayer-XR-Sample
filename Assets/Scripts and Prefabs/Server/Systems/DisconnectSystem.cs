using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Stateful;

[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class DisconnectSystem : SystemBase
{
    //We are going to want to playback adding our "DestroyTag" omponent in EndFixedStepSimEcb
    //similar to adding destroy tags from collisions with bullets
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;
    
    //We will need a query of all entities with NetworkStreamDisconnected components
    private EntityQuery m_DisconnectedNCEQuery;

    protected override void OnCreate()
    {
        //We set our variables
        m_CommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_DisconnectedNCEQuery = GetEntityQuery(ComponentType.ReadWrite<NetworkStreamDisconnected>());

        //We only need to run this if there are disconnected NCEs
        RequireForUpdate(m_DisconnectedNCEQuery);
    }

    protected override void OnUpdate()
    {

        //We need a command buffer because we are making a structural change (adding a DestroyTag)
        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();    

        //There is a dependency on our "ToEntityArrayAsync" because
        //we are depending on this to get done for us to run our .ForEach()
        JobHandle disconnectNCEsDep;

        //We query for all entities that have a NetworkStreamDisconnected component and save it in a native array
        var disconnectedNCEsNative = m_DisconnectedNCEQuery.ToEntityArrayAsync(Allocator.TempJob, out disconnectNCEsDep);
        //We will need to pull the NetworkIdComponent from these entities within our .ForEach so we
        //declare the local variable now
        var getNetworkIdComponentData = GetComponentDataFromEntity<NetworkIdComponent>();

        //We are going to save the JobHandle required from this .ForEach as "cleanPlayersJob"
        //We pass through our native array as read only, and ask .ForEach to dispose of our array on completion
        var cleanPlayersJob = Entities
        .WithReadOnly(disconnectedNCEsNative)
        .WithDisposeOnCompletion(disconnectedNCEsNative)
        .WithAll<PlayerTag>()
        .ForEach((Entity entity, in GhostOwnerComponent ghostOwner) => {

            //We navigate through our disconnected NCE's and see if any player entities match
            for (int i = 0; i < disconnectedNCEsNative.Length; i++)
            {
                if (getNetworkIdComponentData[disconnectedNCEsNative[0]].Value == ghostOwner.NetworkId)
                {
                    //If they do match we add a DestroyTag to delete
                    commandBuffer.AddComponent<DestroyTag>(entity);
                }             
            }
        }).Schedule(JobHandle.CombineDependencies(Dependency, disconnectNCEsDep));

        //We set our Dependency of this sytem to cleanPlayersJob
        Dependency = cleanPlayersJob;

        //And we add our dependency to our command buffer
        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
