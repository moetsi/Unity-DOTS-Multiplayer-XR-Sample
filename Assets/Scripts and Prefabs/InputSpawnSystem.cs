using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;

public partial class InputSpawnSystem : SystemBase
{
    //This will be our query for Players
    private EntityQuery m_PlayerQuery;

    //We will use the BeginSimulationEntityCommandBufferSystem for our structural changes
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;

    //This will save our Player prefab to be used to spawn Players
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        //This is an EntityQuery for our Players, they must have an PlayerTag
        m_PlayerQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerTag>());

        //This will grab the BeginSimulationEntityCommandBuffer system to be used in OnUpdate
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        //Here we set the prefab we will use
        if (m_Prefab == Entity.Null)
        {
            //We grab the converted PrefabCollection Entity's PlayerAuthoringComponent
            //and set m_Prefab to its Prefab value
            m_Prefab = GetSingleton<PlayerAuthoringComponent>().Prefab;

            //we must "return" after setting this prefab because if we were to continue into the Job
            //we would run into errors because the variable was JUST set (ECS funny business)
            //comment out return and see the error
            return;
        }
        byte shoot;
        shoot = 0;
        var playerCount = m_PlayerQuery.CalculateEntityCountWithoutFiltering();

        if (Input.GetKey("space"))
        {
            shoot = 1;
        }

        if (shoot == 1 && playerCount < 1)
        {
            EntityManager.Instantiate(m_Prefab);
            return;
        }
    }
}