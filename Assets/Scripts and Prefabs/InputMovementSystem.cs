using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public partial class InputMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        //We will use playerForce from the GameSettingsComponent to adjust velocity
        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        //we must declare our local variables to be able to use them in the .ForEach() below
        var gameSettings = GetSingleton<GameSettingsComponent>();
        var deltaTime = Time.DeltaTime;

        //we will control thrust with WASD"
        byte right, left, thrust, reverseThrust;
        right = left = thrust = reverseThrust = 0;

        //we will use the mouse to change rotation
        float mouseX = 0;
        float mouseY = 0;

        //we grab "WASD" for thrusting
        if (Input.GetKey("d"))
        {
            right = 1;
        }
        if (Input.GetKey("a"))
        {
            left = 1;
        }
        if (Input.GetKey("w"))
        {
            thrust = 1;
        }
        if (Input.GetKey("s"))
        {
            reverseThrust = 1;
        }
        //we will activate rotating with mouse when the right button is clicked
        if (Input.GetMouseButton(1))
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");

        }

        Entities
        .WithAll<PlayerTag>()
        .ForEach((Entity entity, ref Rotation rotation, ref VelocityComponent velocity) =>
        {
            if (right == 1)
            {   //thrust to the right of where the player is facing
                velocity.Value += (math.mul(rotation.Value, new float3(1,0,0)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (left == 1)
            {   //thrust to the left of where the player is facing
                velocity.Value += (math.mul(rotation.Value, new float3(-1,0,0)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (thrust == 1)
            {   //thrust forward of where the player is facing
                velocity.Value += (math.mul(rotation.Value, new float3(0,0,1)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (reverseThrust == 1)
            {   //thrust backwards of where the player is facing
                velocity.Value += (math.mul(rotation.Value, new float3(0,0,-1)).xyz) *  gameSettings.playerForce * deltaTime;
            }
            if (mouseX != 0 || mouseY != 0)
            {   //move the mouse
                //here we have "hardwired" the look speed, we could have included this in the GameSettingsComponent to make it configurable
                float lookSpeedH = 2f;
                float lookSpeedV = 2f;

                //
                Quaternion currentQuaternion = rotation.Value; 
                float yaw = currentQuaternion.eulerAngles.y;
                float pitch = currentQuaternion.eulerAngles.x;

                //MOVING WITH MOUSE
                yaw += lookSpeedH * mouseX;
                pitch -= lookSpeedV * mouseY;
                Quaternion newQuaternion = Quaternion.identity;
                newQuaternion.eulerAngles = new Vector3(pitch,yaw, 0);
                rotation.Value = newQuaternion;
            }
        }).ScheduleParallel();
    }
}