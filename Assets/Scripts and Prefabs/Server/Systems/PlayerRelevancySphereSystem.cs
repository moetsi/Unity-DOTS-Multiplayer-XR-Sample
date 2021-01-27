using Unity.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSendSystem))]
public class PlayerRelevancySphereSystem : SystemBase
{
    //This will be a struct we use only in this system
    //The ConnectionId is the entities NetworkId
    struct ConnectionRelevancy
    {
        public int ConnectionId;
        public float3 Position;
    }
    //We grab the ghost send system to use its GhostRelevancyMode
    GhostSendSystem m_GhostSendSystem;
    //Here we keep a list of our NCEs with NetworkId and position of player
    NativeList<ConnectionRelevancy> m_Connections;
    EntityQuery m_GhostQuery;
    EntityQuery m_ConnectionQuery;
    protected override void OnCreate()
    {
        m_GhostQuery = GetEntityQuery(ComponentType.ReadOnly<GhostComponent>());
        m_ConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());
        RequireForUpdate(m_ConnectionQuery);
        m_Connections = new NativeList<ConnectionRelevancy>(16, Allocator.Persistent);
        m_GhostSendSystem = World.GetExistingSystem<GhostSendSystem>();
        //We need the GameSettingsComponent so we need to make sure it streamed in from the SubScene
        RequireSingletonForUpdate<GameSettingsComponent>();
    }
    protected override void OnDestroy()
    {
        m_Connections.Dispose();
    }
    protected override void OnUpdate()
    {
        //We only run this if the relevancyRadius is not 0
        var settings = GetSingleton<GameSettingsComponent>();
        if ((int) settings.relevancyRadius == 0)
        {
            m_GhostSendSystem.GhostRelevancyMode = GhostRelevancyMode.Disabled;
            return;
        }
        //This is a special NetCode system configuration
        m_GhostSendSystem.GhostRelevancyMode = GhostRelevancyMode.SetIsIrrelevant;

        //We create a new list of connections ever OnUpdate
        m_Connections.Clear();
        var relevantSet = m_GhostSendSystem.GhostRelevancySet;
        var parallelRelevantSet = relevantSet.AsParallelWriter();

        var maxRelevantSize = m_GhostQuery.CalculateEntityCount() * m_ConnectionQuery.CalculateEntityCount();

        var clearHandle = Job.WithCode(() => {
            relevantSet.Clear();
            if (relevantSet.Capacity < maxRelevantSize)
                relevantSet.Capacity = maxRelevantSize;
        }).Schedule(m_GhostSendSystem.GhostRelevancySetWriteHandle);

        //Here we grab the positions and networkids of the NCEs ComandTargetCommponent's targetEntity
        var connections = m_Connections;
        var transFromEntity = GetComponentDataFromEntity<Translation>(true);
        var connectionHandle = Entities
            .WithReadOnly(transFromEntity)
            .ForEach((in NetworkIdComponent netId, in CommandTargetComponent target) => {
            var pos = new float3();
            //If we havent spawned a player yet we will set the position to the location of the main camera
            if (target.targetEntity == Entity.Null)
                pos = new float3(0,1,-10);
            else 
                pos = transFromEntity[target.targetEntity].Value;
            connections.Add(new ConnectionRelevancy{ConnectionId = netId.Value, Position = pos});
        }).Schedule(Dependency);

        //Here we check all ghosted entities and see which ones are relevant to the NCEs based on distance and the relevancy radius
        Dependency = Entities
            .WithReadOnly(connections)
            .ForEach((in GhostComponent ghost, in Translation pos) => {
            for (int i = 0; i < connections.Length; ++i)
            {
                if (math.distance(pos.Value, connections[i].Position) > settings.relevancyRadius)
                    parallelRelevantSet.TryAdd(new RelevantGhostForConnection(connections[i].ConnectionId, ghost.ghostId), 1);
            }
        }).ScheduleParallel(JobHandle.CombineDependencies(connectionHandle, clearHandle));

        m_GhostSendSystem.GhostRelevancySetWriteHandle = Dependency;
    }
}