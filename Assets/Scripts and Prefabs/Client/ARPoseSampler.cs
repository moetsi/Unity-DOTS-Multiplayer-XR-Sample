using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.XR.ARFoundation;
using Unity.Mathematics;

public class ARPoseSampler : MonoBehaviour
{
    //We will be using ClientSimulationSystemGroup to update our ARPoseComponent
    private ClientSimulationSystemGroup m_ClientSimGroup;
    //We will be using Client World when destroying our SpawnPositionForARComponent
    private World m_ClientWorld;

    //This is the query we will use for SpawnPositionForARComponent
    private EntityQuery m_SpawnPositionQuery;
    //This is the AR Session Origin from the hierarchy that we will use to move the camera
    public ARSessionOrigin m_ARSessionOrigin;
    //We will save our updates to translation and rotation so we can "undo" them before our next update
    //We need to "undo" our updates because of how AR Session Origin MakeContentAppearAt() works
    private float3 m_LastTranslation = new float3(0,0,0); //We set the initial value to 0
    private quaternion m_LastRotation = new quaternion(0,0,0,1);  //We set the initial value to the identity
    
    void Start()
    {
        //We grab ClientSimulationSystemGroup to update ARPoseComponent in our Update loop
        foreach (var world in World.All)
        {
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                //Set our world
                m_ClientWorld = world;
                //We create the ARPoseComponent that we will update with new data
                world.EntityManager.CreateEntity(typeof(ARPoseComponent));
                //We grab the ClientSimulationSystemGroup for our Update loop
                m_ClientSimGroup = world.GetExistingSystem<ClientSimulationSystemGroup>();
                //Now we set our query for SpawnPositionForARComponent
                m_SpawnPositionQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<SpawnPositionForARComponent>());
            }
        }        
    }

    // Update is called once per frame
    void Update()
    {
        //We create a new Translation and Rotation from the transform of the GameObject
        //The GameObject Translation and Rotation is updated by the pose driver
        var arTranslation = new Translation {Value = transform.position};
        var arRotation = new Rotation {Value = transform.rotation};
        //Now we update our ARPoseComponent with the updated Pose Driver data
        var arPose = new ARPoseComponent {
           translation = arTranslation,
           rotation = arRotation 
        };
        m_ClientSimGroup.SetSingleton<ARPoseComponent>(arPose);

        //If the player was spawned, we will move the AR camera to behind the spawn location
        if(!m_SpawnPositionQuery.IsEmptyIgnoreFilter)
        {
            //We grab the component from the Singleton
            var spawnPosition = m_ClientSimGroup.GetSingleton<SpawnPositionForARComponent>();
            // Debug.Log("spawn position is: " + spawnPosition.spawnTranslation.ToString());     
            
            //We set the new pose to behind the player (0, 2, -10) (this is the same value the player is put in front of the pose in InputResponseMovementSystem)
            var newPoseTranslation = (spawnPosition.spawnTranslation) + (math.mul(spawnPosition.spawnRotation, new float3(0,2,0)).xyz) - (math.mul(spawnPosition.spawnRotation, new float3(0,0,10)).xyz);
            //The rotation will be the same
            var newPoseRotation = (spawnPosition.spawnRotation);
            
            // Debug.Log("calculated camera position is: " + newPoseTranslation.ToString());

            //MakeContentAppearAt requires a transform even though it is never used so we create a dummy transform
            Transform dummyTransform = new GameObject().transform;
            //First we will undo our last MakeContentAppearAt to go back to "normal"
            m_ARSessionOrigin.MakeContentAppearAt(dummyTransform, -1f*m_LastTranslation, Quaternion.Inverse(m_LastRotation));
            
            //Now we will update our LastTranslation and LastRotations to the values we are about to use
            //Because of how MakeContentAppearAt works we must do the inverse to move our camera where we want it
            m_LastTranslation = -1f * newPoseTranslation;
            m_LastRotation = Quaternion.Inverse(newPoseRotation);
            //Now that we have set the variables we will use them to adjust the AR pose
            m_ARSessionOrigin.MakeContentAppearAt(dummyTransform, m_LastTranslation, m_LastRotation);

            // Debug.Log("transform after MakeContentAppearAt: " + transform.position.ToString());
            //Now we delete the entity so this only runs during an initial spawn
            m_ClientWorld.EntityManager.DestroyEntity(m_ClientSimGroup.GetSingletonEntity<SpawnPositionForARComponent>());
        }
    }
}