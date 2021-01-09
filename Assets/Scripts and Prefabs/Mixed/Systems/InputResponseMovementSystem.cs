using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Networking.Transport.Utilities;
using Unity.Collections;
using Unity.Physics;
using Unity.Jobs;
using UnityEngine;

//InputResponseMovementSystem runs on both the Client and Server
//It is predicted on the client but "decided" on the server
[UpdateInWorld(UpdateInWorld.TargetWorld.ClientAndServer)] 
public class InputResponseMovementSystem : SystemBase
{
    //This is a special NetCode group that provides a "prediction tick" and a fixed "DeltaTime"
    private GhostPredictionSystemGroup m_PredictionGroup;
    

    protected override void OnCreate()
    {
        // m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        //We will grab this system so we can use its "prediction tick" and "DeltaTime"
        m_PredictionGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        
    }

    protected override void OnUpdate()
    {
        //No need for a CommandBuffer because we are not making any structural changes to any entities
        //We are setting values on components that already exist
        // var commandBuffer = m_BeginSimEcb.CreateCommandBuffer().AsParallelWriter();

        //These are special NetCode values needed to work the prediction system
        var currentTick = m_PredictionGroup.PredictingTick;
        var deltaTime = m_PredictionGroup.Time.DeltaTime;

        //We must declare our local variables before the .ForEach()
        var playerForce = GetSingleton<GameSettingsComponent>().playerForce;

        //We will grab the buffer of player commands from the player entity
        var inputFromEntity = GetBufferFromEntity<PlayerCommand>(true);

        //We are looking for player entities that have PlayerCommands in their buffer
        Entities
        .WithReadOnly(inputFromEntity)
        .WithAll<PlayerTag, PlayerCommand>()
        .ForEach((Entity entity, int nativeThreadIndex, ref Rotation rotation, ref VelocityComponent velocity,
                in GhostOwnerComponent ghostOwner, in PredictedGhostComponent prediction) =>
        {
            //Here we check if we SHOULD do the prediction based on the tick, if we shouldn't, we return
            if (!GhostPredictionSystemGroup.ShouldPredict(currentTick, prediction))
                return;

            //We grab the buffer of commands from the player entity
            var input = inputFromEntity[entity];

            //We then grab the Command from the current tick (which is the PredictingTick)
            //if we cannot get it at the current tick we make sure shoot is 0
            //This is where we will store the current tick data
            PlayerCommand inputData;
            if (!input.GetDataAtTick(currentTick, out inputData))
                inputData.shoot = 0;

            if (inputData.right == 1)
            {   //thrust to the right of where the player is facing
                velocity.Linear += math.mul(rotation.Value, new float3(1,0,0)).xyz * playerForce * deltaTime;                
            }
            if (inputData.left == 1)
            {   //thrust to the left of where the player is facing
                velocity.Linear += math.mul(rotation.Value, new float3(-1,0,0)).xyz * playerForce * deltaTime;
            }
            if (inputData.thrust == 1)
            {   //thrust forward of where the player is facing
                velocity.Linear += math.mul(rotation.Value, new float3(0,0,1)).xyz * playerForce * deltaTime;
            }
            if (inputData.reverseThrust == 1)
            {   //thrust backwards of where the player is facing
                velocity.Linear += math.mul(rotation.Value, new float3(0,0,-1)).xyz * playerForce * deltaTime;
            }

            
            if (inputData.mouseX != 0 || inputData.mouseY != 0)
            {   //move the mouse
                //here we have "hardwired" the look speed, we could have included this in the GameSettingsComponent to make it configurable
                float lookSpeedH = 2f;
                float lookSpeedV = 2f;
                Quaternion currentQuaternion = rotation.Value; 
                float yaw = currentQuaternion.eulerAngles.y;
                float pitch = currentQuaternion.eulerAngles.x;

                //MOVING WITH MOUSE
                yaw += lookSpeedH * inputData.mouseX;
                pitch -= lookSpeedV * inputData.mouseY;
                Quaternion newQuaternion = Quaternion.identity;
                newQuaternion.eulerAngles = new Vector3(pitch,yaw, 0);
                rotation.Value = newQuaternion;
            }
           
        }).ScheduleParallel();

        //No need to .AddJobHandleForProducer() because we did not need a CommandBuffer to make structural changes
    }
    
}