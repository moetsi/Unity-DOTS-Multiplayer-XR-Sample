using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

//We are going to update LATE once all other systems are complete
//because we don't want to destroy the Entity before other systems have
//had a chance to interact with it if they need to
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class AsteroidsDestructionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimEcb;    

    protected override void OnCreate()
    {
        //We grab the EndSimulationEntityCommandBufferSystem to record our structural changes
        m_EndSimEcb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        //We add "AsParallelWriter" when we create our command buffer because we want
        //to run our jobs in parallel
        var commandBuffer = m_EndSimEcb.CreateCommandBuffer().AsParallelWriter();

        //We now any entities with a DestroyTag and an AsteroidTag
        //We could just query for a DestroyTag, but we might want to run different processes
        //if different entities are destroyed, so we made this one specifically for Asteroids
        Entities
        .WithAll<DestroyTag, AsteroidTag>()
        .ForEach((Entity entity, int entityInQueryIndex) =>
        {
            commandBuffer.DestroyEntity(entityInQueryIndex, entity);

        }).ScheduleParallel();

        //We then add the dependencies of these jobs to the EndSimulationEntityCOmmandBufferSystem
        //that will be playing back the structural changes recorded in this sytem
        m_EndSimEcb.AddJobHandleForProducer(Dependency);
    
    }
}