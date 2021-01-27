using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Collections;

//We are going to update LATE once all other systems are complete
//because we don't want to destroy the Entity before other systems have
//had a chance to interact with it if they need to
[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class PlayerDestructionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimEcb;    

    private EntityQuery m_PlayerScores;
    private EntityQuery m_HighestScore;

    protected override void OnCreate()
    {
        //We grab the EndSimulationEntityCommandBufferSystem to record our structural changes
        m_EndSimEcb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        //We set our queries
        m_PlayerScores = GetEntityQuery(ComponentType.ReadWrite<PlayerScoreComponent>());
        m_HighestScore = GetEntityQuery(ComponentType.ReadWrite<HighestScoreComponent>());
    }
    
    protected override void OnUpdate()
    {
        //We add "AsParallelWriter" when we create our command buffer because we want
        //to run our jobs in parallel
        var commandBuffer = m_EndSimEcb.CreateCommandBuffer().AsParallelWriter();

        //We are going to need to update the NCE CommandTargetComponent so we set the argument to false (not read-only)
        var commandTargetFromEntity = GetComponentDataFromEntity<CommandTargetComponent>(false);

        JobHandle playerScoresDep;
        //We grab all the player scores because we don't know who will need to be assigned points
        var playerScoreEntities = m_PlayerScores.ToEntityArrayAsync(Allocator.TempJob, out playerScoresDep);
        //we will need to grab the PlayerScoreComponent from our player score entities to compare values
        var playerScoreComponent = GetComponentDataFromEntity<PlayerScoreComponent>();


        //We now any entities with a DestroyTag and an PlayerTag
        //We could just query for a DestroyTag, but we might want to run different processes
        //if different entities are destroyed, so we made this one specifically for Players
        //We query specifically for players because we need to clear the NCE when they are destroyed
        //In order to write over a variable that we pass through to a job we must include "WithNativeDisableParallelForRestricion"
        //It means "yes we know what we are doing, allow us to write over this variable"
        var playerDestructionJob = Entities
        .WithDisposeOnCompletion(playerScoreEntities)
        .WithNativeDisableParallelForRestriction(playerScoreComponent)
        .WithNativeDisableParallelForRestriction(commandTargetFromEntity)
        .WithAll<DestroyTag, PlayerTag>()
        .ForEach((Entity entity, int nativeThreadIndex, in PlayerEntityComponent playerEntity, in GhostOwnerComponent ghostOwnerComponent) =>
        {
            // Reset the CommandTargetComponent on the Network Connection Entity to the player
            //We are able to find the NCE the player belongs to through the PlayerEntity component
            var state = commandTargetFromEntity[playerEntity.PlayerEntity]; 
            state.targetEntity = Entity.Null;
            commandTargetFromEntity[playerEntity.PlayerEntity] = state;

            //Now we cycle through PlayerScores till we find the right onw
            for (int j = 0; j < playerScoreEntities.Length; j++)
            {
                //Grab the PlayerScore
                var  currentPlayScoreComponent = playerScoreComponent[playerScoreEntities[j]];
                //Check if the player to destroy has the same NetworkId as the current PlayerScore
                if(currentPlayScoreComponent.networkId == ghostOwnerComponent.NetworkId)
                {
                    //We create a new component with updated values
                    var newPlayerScore = new PlayerScoreComponent{
                        networkId = currentPlayScoreComponent.networkId,
                        playerName = currentPlayScoreComponent.playerName,
                        currentScore = 0,
                        highScore = currentPlayScoreComponent.highScore
                        };
                    // SetComponent<PlayerScoreComponent>(playerScoreEntities[j], newPlayerScore);
                    //The reason why we don't go with:
                    //SetComponent<PlayerScoreComponent>(playerScoreEntities[j],  newPlayerScore);
                    //is because SetComponent<PlayerScoreComponent>() gets codegen'd into ComponentDataFromEntity<PlayerScoreComponent>()
                    //and you can't use 2 different ones or else you get an 'two containers may not be the same (aliasing)' error
                    playerScoreComponent[playerScoreEntities[j]] = newPlayerScore;
                }
            }
            //Then destroy the entity
            commandBuffer.DestroyEntity(nativeThreadIndex, entity);

        }).ScheduleParallel(JobHandle.CombineDependencies(Dependency, playerScoresDep));

        //We set the system dependency
        Dependency = playerDestructionJob;
        //We then add the dependencies of these jobs to the EndSimulationEntityCOmmandBufferSystem
        //that will be playing back the structural changes recorded in this sytem
        m_EndSimEcb.AddJobHandleForProducer(Dependency);
    
    }
}