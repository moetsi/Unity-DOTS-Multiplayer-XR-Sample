using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Unity.NetCode;

[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)] 
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
public class MovementSystem : SystemBase
{
    private GhostPredictionSystemGroup m_PredictionGroup;

    protected override void OnCreate()
    {
        m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate()
    {
        var deltaTime = m_PredictionGroup.Time.DeltaTime;
        var currentTick = m_PredictionGroup.PredictingTick;

        Entities
        .ForEach((ref Translation position, in VelocityComponent velocity, in PredictedGhostComponent prediction) =>
        {
            if (!GhostPredictionSystemGroup.ShouldPredict(currentTick, prediction))
                return;

            position.Value.xyz += velocity.Linear * deltaTime;
        }).ScheduleParallel();
    }
}