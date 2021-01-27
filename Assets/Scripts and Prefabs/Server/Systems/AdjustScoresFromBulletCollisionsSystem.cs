using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Stateful;
using UnityEngine;
using Unity.NetCode;


[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class AdjustPlayerScoresFromBulletCollisionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_CommandBufferSystem;
    private TriggerEventConversionSystem m_TriggerSystem;
    private EntityQueryMask m_NonTriggerMask;
    private EntityQuery m_PlayerScores;
    private EntityQuery m_HighestScore;

    protected override void OnCreate()
    {
        m_CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_TriggerSystem = World.GetOrCreateSystem<TriggerEventConversionSystem>();
        
        m_NonTriggerMask = EntityManager.GetEntityQueryMask(
            GetEntityQuery(new EntityQueryDesc
            {
                None = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                }
            })
        );
        //We set our queries
        m_PlayerScores = GetEntityQuery(ComponentType.ReadWrite<PlayerScoreComponent>());
        m_HighestScore = GetEntityQuery(ComponentType.ReadWrite<HighestScoreComponent>());
        //We wait to update until we have our converted entities
        RequireForUpdate(m_PlayerScores);
        RequireForUpdate(m_HighestScore);
    }

    protected override void OnUpdate()
    {
        // Need this extra variable here so that it can
        // be captured by Entities.ForEach loop below
        var nonTriggerMask = m_NonTriggerMask;

        //We grab all the player scores because we don't know who will need to be assigned points
        var playerScoreEntities = m_PlayerScores.ToEntityArray(Allocator.TempJob);
        //we will need to grab the PlayerScoreComponent from our player score entities to compare values
        var playerScoreComponent = GetComponentDataFromEntity<PlayerScoreComponent>();

        //We grab the 1 HighestScore engity
        var highestScoreEntities = m_HighestScore.ToEntityArray(Allocator.TempJob);
        //We will need to grab the HighestScoreComponent from our highest score entity to compare values
        var highestScoreComponent = GetComponentDataFromEntity<HighestScoreComponent>();

        //We are going to use this to pull the GhostOwnerComponent from the bullets to see who they belong to
        var ghostOwner = GetComponentDataFromEntity<GhostOwnerComponent>();
        
        //We need to dispose our entities
        Entities
        .WithDisposeOnCompletion(playerScoreEntities)
        .WithDisposeOnCompletion(highestScoreEntities)
        .WithName("ChangeMaterialOnTriggerEnter")
        .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> triggerEventBuffer) =>
        {
            for (int i = 0; i < triggerEventBuffer.Length; i++)
            {
                //Here we grab our bullet entity and the other entity it collided with
                var triggerEvent = triggerEventBuffer[i];
                var otherEntity = triggerEvent.GetOtherEntity(e); 

                // exclude other triggers and processed events
                if (triggerEvent.State == EventOverlapState.Stay || !nonTriggerMask.Matches(otherEntity))
                {
                    continue;
                }

                //We want our code to run on the first intersection of Bullet and other entity
                else if (triggerEvent.State == EventOverlapState.Enter)
                {

                    //We grab the NetworkId of the bullet so we know who to assign points to
                    var bulletsPlayerNetworkId = ghostOwner[e].NetworkId;

                    //We start with 0 points to add
                    int pointsToAdd = 0;
                    if (HasComponent<PlayerTag>(otherEntity))
                    {
                        //Now we check if the bullet came from the same player
                        if (ghostOwner[otherEntity].NetworkId == bulletsPlayerNetworkId)
                        {
                            //If it is from the same player no points
                            return;
                        }
                        pointsToAdd += 10;
                    }

                    if (HasComponent<AsteroidTag>(otherEntity))
                    {
                        //Bullet hitting an Asteroid is 1 point
                        pointsToAdd += 1;
                    }
                    
                    //After updating the points to add we check the PlayerScore entities and find the one with the
                    //correct NetworkId so we can update the scores for the PlayerScoreComponent
                    //If the updated score is higher than the highest score it updates the highest score
                    for (int j = 0; j < playerScoreEntities.Length; j++)
                    {
                        //Grab the PlayerScore
                        var  currentPlayScoreComponent = playerScoreComponent[playerScoreEntities[j]];
                        if(currentPlayScoreComponent.networkId == bulletsPlayerNetworkId)
                        {
                            //We create a new component with updated values
                            var newPlayerScore = new PlayerScoreComponent{
                                networkId = currentPlayScoreComponent.networkId,
                                playerName = currentPlayScoreComponent.playerName,
                                currentScore = currentPlayScoreComponent.currentScore + pointsToAdd,
                                highScore = currentPlayScoreComponent.highScore
                                };
                            //Here we check if the player beat their own high score
                            if (newPlayerScore.currentScore > newPlayerScore.highScore)
                            {
                                newPlayerScore.highScore = newPlayerScore.currentScore;
                            }

                            //Here we check if the player beat the highest score
                            var currentHighScore = highestScoreComponent[highestScoreEntities[0]];
                            if (newPlayerScore.highScore > currentHighScore.highestScore)
                            {
                                //If it does we make a new HighestScoreComponent
                                var updatedHighestScore = new HighestScoreComponent {
                                    highestScore = newPlayerScore.highScore,
                                    playerName = newPlayerScore.playerName
                                };

                                //The reason why we don't go with:
                                //SetComponent<HighestScoreComponent>(highestScoreEntities[0],  updatedHighestScore);
                                //is because SetComponent<HighestScoreComponent>() gets codegen'd into ComponentDataFromEntity<HighestScoreComponent>()
                                //and you can't use 2 different ones or else you get an 'two containers may not be the same (aliasing)' error
                                highestScoreComponent[highestScoreEntities[0]] = updatedHighestScore;
                            }
                            // SetComponent<PlayerScoreComponent>(playerScoreEntities[j], newPlayerScore);
                            //The reason why we don't go with:
                            //SetComponent<PlayerScoreComponent>(playerScoreEntities[j],  newPlayerScore);
                            //is because SetComponent<PlayerScoreComponent>() gets codegen'd into ComponentDataFromEntity<PlayerScoreComponent>()
                            //and you can't use 2 different ones or else you get an 'two containers may not be the same (aliasing)' error
                            playerScoreComponent[playerScoreEntities[j]] = newPlayerScore;
                        }
                    }
                }
                else
                {
                    continue;
                }
            }
        }).Schedule();
    }
}