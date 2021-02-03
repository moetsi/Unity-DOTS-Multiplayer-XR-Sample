using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine;

//This system should only be run by the server (because the server sends the game settings)
//By sepcifying to update in group ServerSimulationSystemGroup it also specifies that it must
//be run by the server
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(RpcSystem))]
public class ServerReceiveConfirmation : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    protected override void OnCreate()
    {
        //We will be using the BeginSimECB
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        //Requiring the ReceiveRpcCommandRequestComponent ensures that update is only run when an NCE exists and a SendServerGameLoadedRpc2 has come in
        RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<SendServerGameLoadedRpc2>(), ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()));   
    }

    protected override void OnUpdate()
    {

        //We must declare our local variables before using them within a job (.ForEach)
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer();
        var rpcFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();

        Entities
        .ForEach((Entity entity, in SendServerGameLoadedRpc2 request, in ReceiveRpcCommandRequestComponent requestSource) =>
        {
            Debug.Log("server received RPC");
            //This destroys the incoming RPC so the code is only run once
            commandBuffer.DestroyEntity(entity);

            //Check for disconnects before moving forward
            if (!rpcFromEntity.HasComponent(requestSource.SourceConnection))
                return;


            //Here we add 3 components to the NCE
            //The first, PlayerSpawningStateComonent will be used during our player spawn flow
            commandBuffer.AddComponent(requestSource.SourceConnection, new PlayerSpawningStateComponent());
            //NetworkStreamInGame must be added to an NCE to start receiving Snapshots
            commandBuffer.AddComponent(requestSource.SourceConnection, default(NetworkStreamInGame));
            //GhostConnectionPosition is added to be used in conjunction with GhostDistanceImportance (from the socket section)
            commandBuffer.AddComponent(requestSource.SourceConnection, default(GhostConnectionPosition));

        }).Schedule();

        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}