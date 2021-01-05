using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;
using Unity;
using System;

#if UNITY_EDITOR
using Unity.NetCode.Editor;
#endif


//ServerConnectionControl is run in ServerWorld and starts listening on a port
//The port is provided by the ServerDataComponent
[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
public class ServerConnectionControl : SystemBase
{
    private ushort m_GamePort;

    protected override void OnCreate()
    {
        // We require the InitializeServerComponent to be created before OnUpdate runs
        RequireSingletonForUpdate<InitializeServerComponent>();
        
    }

    protected override void OnUpdate()
    {
        //load up data to be used OnUpdate
        var serverDataEntity = GetSingletonEntity<ServerDataComponent>();
        var serverData = EntityManager.GetComponentData<ServerDataComponent>(serverDataEntity);
        m_GamePort = serverData.GamePort;

        //We destroy the InitializeServerComponent so this system only runs once
        EntityManager.DestroyEntity(GetSingletonEntity<InitializeServerComponent>());

        // This is used to split up the game's "world" into sections ("tiles")
        // The client is in a "tile" and networked objects are in "tiles"
        // the client is streamed data based on tiles that are near them
        //https://docs.unity3d.com/Packages/com.unity.netcode@0.5/manual/ghost-snapshots.html
        //check out "Distance based importance" in the link above
        var grid = EntityManager.CreateEntity();
        EntityManager.AddComponentData(grid, new GhostDistanceImportance
        {
            ScaleImportanceByDistance = GhostDistanceImportance.DefaultScaleFunctionPointer,
            TileSize = new int3(80, 80, 80),
            TileCenter = new int3(0, 0, 0),
            TileBorderWidth = new float3(1f, 1f, 1f)
        });

        //Here is where the server creates a port and listens
        NetworkEndPoint ep = NetworkEndPoint.AnyIpv4;
        ep.Port = m_GamePort;
        World.GetExistingSystem<NetworkStreamReceiveSystem>().Listen(ep);
        Debug.Log("Server is listening on port: " + m_GamePort.ToString());
    }
}

//ClientConnectionControl is run in ClientWorld and connects to an IP address and port
//The IP address and port is provided by the ClientDataComponent
[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
public class ClientConnectionControl : SystemBase
{
    private string m_ConnectToServerIp;
    private ushort m_GamePort;

    protected override void OnCreate()
    {
        // We require the component to be created before OnUpdate runs
        RequireSingletonForUpdate<InitializeClientComponent>();

    }

    protected override void OnUpdate()
    {
        //load up data to be used OnUpdate
        var clientDataEntity = GetSingletonEntity<ClientDataComponent>();
        var clientData = EntityManager.GetComponentData<ClientDataComponent>(clientDataEntity);
        
        m_ConnectToServerIp = clientData.ConnectToServerIp.ToString();
        m_GamePort = clientData.GamePort;

        // As soon as this runs, the component is destroyed so it doesn't happen twice
        EntityManager.DestroyEntity(GetSingletonEntity<InitializeClientComponent>());

        NetworkEndPoint ep = NetworkEndPoint.Parse(m_ConnectToServerIp, m_GamePort);
        World.GetExistingSystem<NetworkStreamReceiveSystem>().Connect(ep);
        Debug.Log("Client connecting to ip: " + m_ConnectToServerIp + " and port: " + m_GamePort.ToString());
    }
}