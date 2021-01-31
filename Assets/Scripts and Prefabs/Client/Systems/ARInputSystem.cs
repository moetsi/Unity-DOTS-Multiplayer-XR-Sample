using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.XR;
using Unity.Physics;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public class ARInputSystem : SystemBase
{
    //We will need a command buffer for structural changes
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;
    //We will grab the ClientSimulationSystemGroup because we require its tick in the ICommandData
    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;

    protected override void OnCreate()
    {
        //We set our variables
        m_ClientSimulationSystemGroup = World.GetOrCreateSystem<ClientSimulationSystemGroup>();
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        //We will only run this system if the player is in game and if the palyer is an AR player
        RequireSingletonForUpdate<NetworkStreamInGame>();
        RequireSingletonForUpdate<IsARPlayerComponent>();
    }

    protected override void OnUpdate()
    {
        //The only inputs is for shooting or for self destruction
        //Movement will be through the ARPoseComponent
        byte selfDestruct, shoot;
        selfDestruct = shoot = 0;

        //More than 2 touches will register as self-destruct
        if (Input.touchCount > 2)
        {
            selfDestruct = 1;
        }
        //A single touch will register as shoot
        if (Input.touchCount == 1)
        {
            shoot = 1;
        }

        //We grab the AR pose to send to the server for movement
        var arPoseDriver = GetSingleton<ARPoseComponent>();
        //We must declare our local variables before the .ForEach()
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer().AsParallelWriter();
        var inputFromEntity = GetBufferFromEntity<PlayerCommand>();
        var inputTargetTick = m_ClientSimulationSystemGroup.ServerTick;

        Entities
        .WithAll<OutgoingRpcDataStreamBufferComponent>()
        .WithNone<NetworkStreamDisconnected>()
        .ForEach((Entity entity, int nativeThreadIndex, in CommandTargetComponent state) =>
        {
            //This is the same pattern as a desktop player
            //If targetEntity is null we spawn a new player
            if (state.targetEntity == Entity.Null)
            {
                if (shoot != 0)
                {
                    var req = commandBuffer.CreateEntity(nativeThreadIndex);
                    commandBuffer.AddComponent<PlayerSpawnRequestRpc>(nativeThreadIndex, req);
                    commandBuffer.AddComponent(nativeThreadIndex, req, new SendRpcCommandRequestComponent {TargetConnection = entity});
                }
            }
            else
            {
                //We provide our PlayerCommand to be sent to the server
                if (inputFromEntity.HasComponent(state.targetEntity))
                {
                    var input = inputFromEntity[state.targetEntity];
                    input.AddCommandData(new PlayerCommand{Tick = inputTargetTick,
                    selfDestruct = selfDestruct, shoot = shoot,
                    isAR = 1,
                    arTranslation = arPoseDriver.translation.Value,
                    arRotation = arPoseDriver.rotation.Value});
                }
            }
        }).Schedule();
        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}