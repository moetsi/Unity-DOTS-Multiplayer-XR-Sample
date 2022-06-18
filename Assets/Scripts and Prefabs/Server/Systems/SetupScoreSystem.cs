using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

//The server will be keeping score
//The client will only read the scores and update overlay
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public partial class SetupScoreSystem : SystemBase
{
    //We will be making structural changes so we need a command buffer
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    //This will be the query for the highest score
    private EntityQuery m_HighestScoreQuery;

    //This will be the query for the player scores
    private EntityQuery m_PlayerScoresQuery;

    //This will be the prefab used to create PlayerScores
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        //We set the command buffer
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        //This will be used to check if there is already HighestScore (initialization)
        m_HighestScoreQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HighestScoreComponent>());

        //This will be used to check if there are already PlayerScores (initialization)
        m_PlayerScoresQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerScoreComponent>());

        //We are going to wait to initialize and update until the first player connects and sends their name
        RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<SendServerPlayerNameRpc>(), ComponentType.ReadOnly<ReceiveRpcCommandRequestComponent>()));        
    }

    protected override void OnUpdate()
    {
        //Here we set the prefab we will use
        if (m_Prefab == Entity.Null)
        {
            //We grab the converted PrefabCollection Entity's PlayerScoreAuthoringComponent
            //and set m_Prefab to its Prefab value
            m_Prefab = GetSingleton<PlayerScoreAuthoringComponent>().Prefab;
            //We then initialize by creating the first PlayerScore
            var initialPlayerScore = EntityManager.Instantiate(m_Prefab);
            //We set the initial player score to 1 so the first player will be assigned this PlayerScore
            EntityManager.SetComponentData<PlayerScoreComponent>(initialPlayerScore, new PlayerScoreComponent{
                networkId = 1
            });
            //we must "return" after setting this prefab because if we were to continue into the Job
            //we would run into errors because the variable was JUST set (ECS funny business)
            //comment out return and see the error
            return;
        }
        
        //We need to declare our local variables before the .ForEach()
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer();
        //We use this to check for disconnects
        var rpcFromEntity = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>();
        //We are going to grab all existing player scores because we need to check if the new player has an old NetworkId
        var currentPlayerScoreEntities = m_PlayerScoresQuery.ToEntityArray(Allocator.TempJob);
        //We are going to need to grab the Player score from the entity
        var playerScoreComponent = GetComponentDataFromEntity<PlayerScoreComponent>();
        //We grab the prefab in case we need to create a new PlayerScore for a new NetworkId
        var scorePrefab = m_Prefab;
        //We are going to need to be able to grab the NetworkIdComponent from the RPC source to know what the player's NetworkId is
        var networkIdFromEntity = GetComponentDataFromEntity<NetworkIdComponent>();
        
        Entities
        .WithDisposeOnCompletion(currentPlayerScoreEntities)
        .ForEach((Entity entity, in SendServerPlayerNameRpc request, in ReceiveRpcCommandRequestComponent requestSource) =>
        {
            //Delete the rpc
            commandBuffer.DestroyEntity(entity);
            
            //Check for disconnects
            if (!rpcFromEntity.HasComponent(requestSource.SourceConnection))
                return;

            //Grab the NetworkIdComponent's Value
            var newPlayersNetworkId = networkIdFromEntity[requestSource.SourceConnection].Value;

            //We create a clean PlayerScore component with the player's name and the player's NetworkId value
            var newPlayerScore = new PlayerScoreComponent{
                networkId = newPlayersNetworkId,
                playerName = request.playerName,
                currentScore = 0,
                highScore = 0
            };

            //Now we are going to check all current PlayerScores and see if this NetworkId has been used before
            //If it has we set it to our new PlayerScoreComponent
            bool uniqueNetworkId = true;
            for (int i = 0; i < currentPlayerScoreEntities.Length; i++)
            {
                //We call the data componentData just to make it more legible on the if() line
                var componentData = playerScoreComponent[currentPlayerScoreEntities[i]];
                if(componentData.networkId == newPlayersNetworkId)
                {
                    commandBuffer.SetComponent<PlayerScoreComponent>(currentPlayerScoreEntities[i], newPlayerScore);
                    uniqueNetworkId = false;
                }
                
            }
            //If this NetworkId has not been used before we create a new PlayerScore
            if (uniqueNetworkId)
            {
                var playerScoreEntity = commandBuffer.Instantiate(scorePrefab);
                //We set the initial player score to 1 so the first player will be assigned this PlayerScore
                commandBuffer.SetComponent<PlayerScoreComponent>(playerScoreEntity, newPlayerScore);
            }
            
        }).Schedule();
    }
}