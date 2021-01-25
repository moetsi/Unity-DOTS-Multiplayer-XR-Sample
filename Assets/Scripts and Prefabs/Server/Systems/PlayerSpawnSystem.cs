using System.Diagnostics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

//This tag is only used by the systems in this file so we define it here
public struct PlayerSpawnInProgressTag : IComponentData
{
}

//Only the server will be running this system to spawn the player
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class PlayerSpawnSystem : SystemBase
{

    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();


        //We check to ensure GameSettingsComponent exists to know if the SubScene has been streamed in
        //We need the SubScene for actions in our OnUpdate()
        RequireSingletonForUpdate<GameSettingsComponent>(); 
    }

    protected override void OnUpdate()
    {
        //Here we set the prefab we will use
        if (m_Prefab == Entity.Null)
        {
            //We grab the converted PrefabCollection Entity's PlayerAuthoringComponent
            //and set m_Prefab to its Prefab value
            m_Prefab = GetSingleton<PlayerAuthoringComponent>().Prefab;
            //we must "return" after setting this prefab because if we were to continue into the Job
            //we would run into errors because the variable was JUST set (ECS funny business)
            //comment out return and see the error
            return;
        }

        //Because of how ECS works we must declare local variables that will be used within the job
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer();
        var playerPrefab = m_Prefab;
        var rand = new Unity.Mathematics.Random((uint) Stopwatch.GetTimestamp());
        var gameSettings = GetSingleton<GameSettingsComponent>();

        //GetComponentDataFromEntity allows us to grab data from an entity that we don't have access to
        //until we are within a job
        //We know we will need to get the PlayerSpawningStateComponent from an NCE but we don't know which one yet
        //So we create a variable that will get PlayerSpawningStateComponent from an entity
        var playerStateFromEntity = GetComponentDataFromEntity<PlayerSpawningStateComponent>();

        //Similar to playerStateFromEntity, these variables WILL get data from an entity (in the job below)
        //but do not have it currently
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>();
        var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>();

        //We are looking for NCEs with a PlayerSpawnRequestRpc
        //That means the client associated with that NCE wants a player to be spawned for them
        Entities
        .ForEach((Entity entity, in PlayerSpawnRequestRpc request,
            in ReceiveRpcCommandRequestComponent requestSource) =>
        {
            //We immediately destroy the request so we act on it once
            commandBuffer.DestroyEntity(entity);

            //These are checks to see if the NCE has disconnected or if there are any other issues
            //These checks are pulled from Unity samples and we have left them in even though they seem
            //Is there a PlayerSpawningState on the NCE
            //Is there a CommandTargetComponent on the NCE
            //Is the CommandTargetComponent targetEntity == Entity.Null
            //Is the PlayerSpawningState == 0
            //If all those are true we continue with spawning, otherwise we don't

            if (!playerStateFromEntity.HasComponent(requestSource.SourceConnection) ||
                !commandTargetFromEntity.HasComponent(requestSource.SourceConnection) ||
                commandTargetFromEntity[requestSource.SourceConnection].targetEntity != Entity.Null ||
                playerStateFromEntity[requestSource.SourceConnection].IsSpawning != 0)
                return;

            //We create our player prefab
            var player = commandBuffer.Instantiate(playerPrefab);

            //We will spawn our player in the center-ish of our game
            var width = gameSettings.levelWidth * .2f;
            var height = gameSettings.levelHeight * .2f;
            var depth = gameSettings.levelDepth * .2f;
            

            var pos = new Translation
            {
                Value = new float3(rand.NextFloat(-width, width),
                    rand.NextFloat(-height, height), rand.NextFloat(-depth, depth))
            };

            //We will not spawn a random rotation for simplicity but include
            //setting rotation for you to be able to update in your own projects if you like
            var rot = new Rotation {Value = Quaternion.identity};

            //Here we set the componets that already exist on the Player prefab
            commandBuffer.SetComponent(player, pos);
            commandBuffer.SetComponent(player, rot);
            //This sets the GhostOwnerComponent value to the NCE NetworkId
            commandBuffer.SetComponent(player, new GhostOwnerComponent {NetworkId = networkIdFromEntity[requestSource.SourceConnection].Value});
            //This sets the PlayerEntity value in PlayerEntityComponent to the NCE
            commandBuffer.SetComponent(player, new PlayerEntityComponent {PlayerEntity = requestSource.SourceConnection});

            //Here we add a component that was not included in the Player prefab, PlayerSpawnInProgressTag
            //This is a temporary tag used to make sure the entity was able to be created and will be removed
            //in PlayerCompleteSpawnSystem below    
            commandBuffer.AddComponent(player, new PlayerSpawnInProgressTag());

            //We update the PlayerSpawningStateComponent tag on the NCE to "currently spawning" (1)
            playerStateFromEntity[requestSource.SourceConnection] = new PlayerSpawningStateComponent {IsSpawning = 1};
        }).Schedule();


        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}

//We want to complete the spawn before ghosts are sent on the server
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSendSystem))]
public class PlayerCompleteSpawnSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer();

        //GetComponentDataFromEntity allows us to grab data from an entity that we don't have access to
        //until we are within a job
        //We don't know exactly which NCE we currently want to grab data from, but we do know we will want to
        //so we use GetComponentDataFromEntity to prepare ECS that we will be grabbing this data from an entity
        var playerStateFromEntity = GetComponentDataFromEntity<PlayerSpawningStateComponent>();
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>();
        var connectionFromEntity = GetComponentDataFromEntity<NetworkStreamConnection>();

        Entities.WithAll<PlayerSpawnInProgressTag>().
            ForEach((Entity entity, in PlayerEntityComponent player) =>
            {
                //This is another check from Unity samples
                //This ensures there was no disconnect
                if (!playerStateFromEntity.HasComponent(player.PlayerEntity) ||
                    !connectionFromEntity[player.PlayerEntity].Value.IsCreated)
                {
                    //Player was disconnected during spawn, or other error so delete
                    commandBuffer.DestroyEntity(entity);
                    return;
                }

                //If there was no error with spawning the player we can remove the PlayerSpawnInProgressTag
                commandBuffer.RemoveComponent<PlayerSpawnInProgressTag>(entity);

                //We now update the NCE to point at our player entity
                commandTargetFromEntity[player.PlayerEntity] = new CommandTargetComponent {targetEntity = entity};
                //We can now say that our player is no longer spawning so we set IsSpawning = 0 on the NCE
                playerStateFromEntity[player.PlayerEntity] = new PlayerSpawningStateComponent {IsSpawning = 0};
            }).Schedule();
            
        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}