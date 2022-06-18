using Unity.NetCode;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSendSystem))]
public partial class PlayerRelevancySphereSystem : SystemBase
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
        //This is saying that any ghost we put in this list is IRRELEVANT (it means ignore these ghosts)
        m_GhostSendSystem.GhostRelevancyMode = GhostRelevancyMode.SetIsIrrelevant;

        //We create a new list of connections ever OnUpdate
        m_Connections.Clear();
        var irrelevantSet = m_GhostSendSystem.GhostRelevancySet;
        //This is our irrelevantSet that we will be using to add to our list
        var parallelIsNotRelevantSet = irrelevantSet.AsParallelWriter();

        var maxRelevantSize = m_GhostQuery.CalculateEntityCount() * m_ConnectionQuery.CalculateEntityCount();

        var clearHandle = Job.WithCode(() => {
            irrelevantSet.Clear();
            if (irrelevantSet.Capacity < maxRelevantSize)
                irrelevantSet.Capacity = maxRelevantSize;
        }).Schedule(m_GhostSendSystem.GhostRelevancySetWriteHandle);

        //Here we grab the positions and networkids of the NCEs ComandTargetCommponent's targetEntity
        var connections = m_Connections;
        var transFromEntity = GetComponentDataFromEntity<Translation>(true);
        var connectionHandle = Entities
            .WithReadOnly(transFromEntity)
            .WithNone<NetworkStreamDisconnected>()
            .WithAll<NetworkStreamInGame>()
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
            .ForEach((Entity entity, in GhostComponent ghost, in Translation pos) => {
            for (int i = 0; i < connections.Length; ++i)
            {
                //Here we do a check on distance, and if the entity is a PlayerScore or HighestScore entity
                //If the ghost is out of the radius (and is not PlayerScore or HighestScore) then we add it to the "ignore this ghost" list (parallelIsNotRelevantSet)
                if (math.distance(pos.Value, connections[i].Position) > settings.relevancyRadius && !(HasComponent<PlayerScoreComponent>(entity) || HasComponent<HighestScoreComponent>(entity)))
                    parallelIsNotRelevantSet.TryAdd(new RelevantGhostForConnection(connections[i].ConnectionId, ghost.ghostId), 1);
            }
        }).ScheduleParallel(JobHandle.CombineDependencies(connectionHandle, clearHandle));

        m_GhostSendSystem.GhostRelevancySetWriteHandle = Dependency;
    }
}