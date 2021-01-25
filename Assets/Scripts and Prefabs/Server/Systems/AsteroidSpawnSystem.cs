using System.Diagnostics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Unity.Physics;
using Unity.NetCode;

//Asteroid spawning will occur on the server
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class AsteroidSpawnSystem : SystemBase
{
    //This will be our query for Asteroids
    private EntityQuery m_AsteroidQuery;

    //We will use the BeginSimulationEntityCommandBufferSystem for our structural changes
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    //This will be our query to find GameSettingsComponent data to know how many and where to spawn Asteroids
    private EntityQuery m_GameSettingsQuery;

    //This will save our Asteroid prefab to be used to spawn Asteroids
    private Entity m_Prefab;

    //This is the query for checking network connections with clients
    private EntityQuery m_ConnectionGroup;

    protected override void OnCreate()
    {
        //This is an EntityQuery for our Asteroids, they must have an AsteroidTag
        m_AsteroidQuery = GetEntityQuery(ComponentType.ReadWrite<AsteroidTag>());

        //This will grab the BeginSimulationEntityCommandBuffer system to be used in OnUpdate
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();

        //This is an EntityQuery for the GameSettingsComponent which will drive how many Asteroids we spawn
        m_GameSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<GameSettingsComponent>());

        //This says "do not go to the OnUpdate method until an entity exists that meets this query"
        //We are using GameObjectConversion to create our GameSettingsComponent so we need to make sure 
        //The conversion process is complete before continuing
        RequireForUpdate(m_GameSettingsQuery);

        //This will be used to check how many connected clients there are
        //If there are no connected clients the server will not spawn asteroids to save CPU
        m_ConnectionGroup = GetEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>());
    }
    
    protected override void OnUpdate()
    {
        //Here we check the amount of connected clients
        if (m_ConnectionGroup.IsEmptyIgnoreFilter)
        {
            // No connected players, just destroy all asteroids to save CPU
            EntityManager.DestroyEntity(m_AsteroidQuery);
            return;
        }

        //Here we set the prefab we will use
        if (m_Prefab == Entity.Null)
        {
            //We grab the converted PrefabCollection Entity's AsteroidAuthoringComponent
            //and set m_Prefab to its Prefab value
            m_Prefab = GetSingleton<AsteroidAuthoringComponent>().Prefab;
            //we must "return" after setting this prefab because if we were to continue into the Job
            //we would run into errors because the variable was JUST set (ECS funny business)
            //comment out return and see the error
            return;
        }

        //Because of how ECS works we must declare local variables that will be used within the job
        //You cannot "GetSingleton<GameSettingsComponent>()" from within the job, must be declared outside
        var settings = GetSingleton<GameSettingsComponent>();

        //Here we create our commandBuffer where we will "record" our structural changes (creating an Asteroid)
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();

        //This provides the current amount of Asteroids in the EntityQuery
        var count = m_AsteroidQuery.CalculateEntityCountWithoutFiltering();

        //We must declare our prefab as a local variable (ECS funny business)
        var asteroidPrefab = m_Prefab;

        //We will use this to generate random positions
        var rand = new Unity.Mathematics.Random((uint)Stopwatch.GetTimestamp());

        Job
        .WithCode(() => {
            for (int i = count; i < settings.numAsteroids; ++i)
            {
                // this is how much within perimeter asteroids start
                var padding = 0.1f;

                // we are going to have the asteroids start on the perimeter of the level
                // choose the x, y, z coordinate of perimeter
                // so the x value must be from negative levelWidth/2 to positive levelWidth/2 (within padding)
                var xPosition = rand.NextFloat(-1f*((settings.levelWidth)/2-padding), (settings.levelWidth)/2-padding);
                // so the y value must be from negative levelHeight/2 to positive levelHeight/2 (within padding)
                var yPosition = rand.NextFloat(-1f*((settings.levelHeight)/2-padding), (settings.levelHeight)/2-padding);
                // so the z value must be from negative levelDepth/2 to positive levelDepth/2 (within padding)
                var zPosition = rand.NextFloat(-1f*((settings.levelDepth)/2-padding), (settings.levelDepth)/2-padding);
                
                //We now have xPosition, yPostiion, zPosition in the necessary range
                //With "chooseFace" we will decide which face of the cube the Asteroid will spawn on
                var chooseFace = rand.NextFloat(0,6);
                
                //Based on what face was chosen, we x, y or z to a perimeter value
                //(not important to learn ECS, just a way to make an interesting prespawned shape)
                if (chooseFace < 1) {xPosition = -1*((settings.levelWidth)/2-padding);}
                else if (chooseFace < 2) {xPosition = (settings.levelWidth)/2-padding;}
                else if (chooseFace < 3) {yPosition = -1*((settings.levelHeight)/2-padding);}
                else if (chooseFace < 4) {yPosition = (settings.levelHeight)/2-padding;}
                else if (chooseFace < 5) {zPosition = -1*((settings.levelDepth)/2-padding);}
                else if (chooseFace < 6) {zPosition = (settings.levelDepth)/2-padding;}

                //we then create a new translation component with the randomly generated x, y, and z values                
                var pos = new Translation{Value = new float3(xPosition, yPosition, zPosition)};

                //on our command buffer we record creating an entity from our Asteroid prefab
                var e = commandBuffer.Instantiate(asteroidPrefab);

                //we then set the Translation component of the Asteroid prefab equal to our new translation component
                commandBuffer.SetComponent(e, pos);

                //We will now set the PhysicsVelocity of our asteroids
                //here we generate a random Vector3 with x, y and z between -1 and 1
                var randomVel = new Vector3(rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f), rand.NextFloat(-1f, 1f));
                //next we normalize it so it has a magnitude of 1
                randomVel.Normalize();
                //now we set the magnitude equal to the game settings
                randomVel = randomVel * settings.asteroidVelocity;
                //here we create a new VelocityComponent with the velocity data
                var vel = new PhysicsVelocity{Linear = new float3(randomVel.x, randomVel.y, randomVel.z)};
                //now we set the velocity component in our asteroid prefab
                commandBuffer.SetComponent(e, vel);

            }
        }).Schedule();

        //This will add our dependency to be played back on the BeginSimulationEntityCommandBuffer
        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}