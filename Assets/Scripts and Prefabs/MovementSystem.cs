using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

public partial class MovementSystem : SystemBase
{

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        Entities
        .ForEach((ref Translation position, in VelocityComponent velocity) =>
        {
            position.Value.xyz += velocity.Value * deltaTime;
        }).ScheduleParallel();
    }
}