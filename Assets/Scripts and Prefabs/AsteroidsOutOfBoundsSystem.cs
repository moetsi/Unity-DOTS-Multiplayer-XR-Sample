using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
//We are adding this system within the FixedStepSimulationGroup
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(EndFixedStepSimulationEntityCommandBufferSystem))] 
public partial class AsteroidsOutOfBoundsSystem : SystemBase
{
    //We are going to use the EndFixedStepSimECB
    //This is because when we use Unity Physics our physics will run in the FixedStepSimulationSystem
    //We are dipping our toes into placing our systems in specific system groups
    //The FixedStepSimGroup has its own EntityCommandBufferSystem we will use to make the structural change
    //of adding the DestroyTag
    private EndFixedStepSimulationEntityCommandBufferSystem m_EndFixedStepSimECB;
    protected override void OnCreate()
    {
        //We grab the EndFixedStepSimECB for our OnUpdate
        m_EndFixedStepSimECB = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        //We want to make sure we don't update until we have our GameSettingsComponent
        //because we need the data from this component to know where the perimeter of our cube is
        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        //We want to run this as parallel jobs so we need to add "AsParallelWriter" when creating
        //our command buffer
        var commandBuffer = m_EndFixedStepSimECB.CreateCommandBuffer().AsParallelWriter();
        //We must declare our local variables that we will use in our job
        var settings = GetSingleton<GameSettingsComponent>();
        //This time we query entities with components by using "WithAll" tag
        //This makes sure that we only grab entities with an AsteroidTag component so we don't affect other entities
        //that might have passed the perimeter of the cube  
        Entities
        .WithAll<AsteroidTag>()
        .ForEach((Entity entity, int entityInQueryIndex, in Translation position) =>
        {
            //We check if the current Translation value is out of bounds
            if (Mathf.Abs(position.Value.x) > settings.levelWidth/2 ||
                Mathf.Abs(position.Value.y) > settings.levelHeight/2 ||
                Mathf.Abs(position.Value.z) > settings.levelDepth/2)
            {
                //If it is out of bounds wee add the DestroyTag component to the entity and return
                commandBuffer.AddComponent(entityInQueryIndex, entity, new DestroyTag());
                return;
            }
        }).ScheduleParallel();
        //We add the dependencies to the CommandBuffer that will be playing back these structural changes (adding a DestroyTag)
        m_EndFixedStepSimECB.AddJobHandleForProducer(Dependency);
    }
}