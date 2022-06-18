using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Collections;

public class ClientServerConnectionHandler : MonoBehaviour
{
    //This is the store of server/client info
    public ClientServerInfo ClientServerInfo;

    //These are the launch objects from Navigation scene that tells what to set up
    private GameObject[] launchObjects;

    //These will gets access to the UI views 
    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;

    //We will use these variables for hitting Quit Game on client or if server disconnects
    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;
    private World m_ClientWorld;
    private EntityQuery m_ClientNetworkIdComponentQuery;
    private EntityQuery m_ClientDisconnectedNCEQuery;

    //We will use these variables for hitting Quit Game on server
    private World m_ServerWorld;
    private EntityQuery m_ServerNetworkIdComponentQuery;

    void OnEnable()
    {
        //This will put callback on "Quit Game" button
        //This triggers the clean up function (ClickedQuitGame)
        m_GameManagerUIVE = m_GameUIDocument.rootVisualElement;
        m_GameManagerUIVE.Q("quit-game")?.RegisterCallback<ClickEvent>(ev => ClickedQuitGame());
    }

    void Awake()
    {
        launchObjects = GameObject.FindGameObjectsWithTag("LaunchObject");
        foreach(GameObject launchObject in launchObjects)
        {
            ///  
            //Checks for server launch object
            //If it exists it creates ServerDataComponent InitializeServerComponent and
            //passes through server data to ClientServerInfo
            // 
            if(launchObject.GetComponent<ServerLaunchObjectData>() != null)
            {
                //This sets the gameobject server data  in ClientServerInfo (mono)
                ClientServerInfo.IsServer = true;
                ClientServerInfo.GameName = launchObject.GetComponent<ServerLaunchObjectData>().GameName;
                ClientServerInfo.BroadcastIpAddress = launchObject.GetComponent<ServerLaunchObjectData>().BroadcastIpAddress;
                ClientServerInfo.BroadcastPort = launchObject.GetComponent<ServerLaunchObjectData>().BroadcastPort;

                //This sets the component server data in server world(dots)
                //ClientServerConnectionControl (server) will run in server world
                //it will pick up this component and use it to listen on the port
                foreach (var world in World.All)
                {
                    //we cycle through all the worlds, and if the world has ServerSimulationSystemGroup
                    //we move forward (because that is the server world)
                    if (world.GetExistingSystem<ServerSimulationSystemGroup>() != null)
                    {
                        var ServerDataEntity = world.EntityManager.CreateEntity();
                        world.EntityManager.AddComponentData(ServerDataEntity, new ServerDataComponent
                        {
                            GameName = ClientServerInfo.GameName,
                            GamePort = ClientServerInfo.GamePort
                        });
                        //Create component that allows server initialization to run
                        world.EntityManager.CreateEntity(typeof(InitializeServerComponent));

                        //For handling server disconnecting by hitting the quit button
                        m_ServerWorld = world;
                        m_ServerNetworkIdComponentQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());

                    }
                }
            }

            // 
            //Checks for client launch object
            //If it exists it creates ClientDataComponent, InitializeServerComponent and
            // passes through client data to ClientServerInfo
            // 
            if(launchObject.GetComponent<ClientLaunchObjectData>() != null)
            {
                //This sets the gameobject data in ClientServerInfo (mono)
                ClientServerInfo.IsClient = true;
                ClientServerInfo.ConnectToServerIp = launchObject.GetComponent<ClientLaunchObjectData>().IPAddress;                
                ClientServerInfo.PlayerName = launchObject.GetComponent<ClientLaunchObjectData>().PlayerName;

                //This sets the component client data in server world (dots)
                //ClientServerConnectionControl (client) will run in client world
                //it will pick up this component and use it connect to IP and port
                foreach (var world in World.All)
                {
                    //We cycle through all the worlds, and if the world has ClientSimulationSystemGroup
                    //we move forward (because that is the client world)
                    if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                    {
                        var ClientDataEntity = world.EntityManager.CreateEntity();
                        world.EntityManager.AddComponentData(ClientDataEntity, new ClientDataComponent
                        {
                            PlayerName = ClientServerInfo.PlayerName,
                            ConnectToServerIp = ClientServerInfo.ConnectToServerIp,
                            GamePort = ClientServerInfo.GamePort
                        });
                        //Create component that allows client initialization to run
                        world.EntityManager.CreateEntity(typeof(InitializeClientComponent));

                        //We will now set the variables we need to clean up during QuitGame()
                        m_ClientWorld = world;
                        m_ClientSimulationSystemGroup = world.GetExistingSystem<ClientSimulationSystemGroup>();
                        m_ClientNetworkIdComponentQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());
                        //This variable is used to check if the server disconnected
                        m_ClientDisconnectedNCEQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDisconnected>());

                    }
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //The client checks if the NCE has a NetworkStreamDisconnected component
        //If it does we act like they quit the game manually
        if(m_ClientDisconnectedNCEQuery.IsEmptyIgnoreFilter)
            return;
        else
            ClickedQuitGame();
    }

   //This function will navigate us to NavigationScene and connected with the clients/server about leaving
    void ClickedQuitGame()
    {
        //As a client if we were able to create an NCE we must add a request disconnect
        if (!m_ClientNetworkIdComponentQuery.IsEmptyIgnoreFilter)
        {
            var clientNCE = m_ClientSimulationSystemGroup.GetSingletonEntity<NetworkIdComponent>();
            m_ClientWorld.EntityManager.AddComponentData(clientNCE, new NetworkStreamRequestDisconnect());

        }

        //As a server if we were able to create an NCE we must add a request disconnect to all NCEs
        //We must to see if this was a host build
        if (m_ServerWorld != null)
        {
            //First we grab the array of NCEs
            var nceArray = m_ServerNetworkIdComponentQuery.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < nceArray.Length; i++)
            {
                //Then we add our NetworkStreamDisconnect component to tell the clients we are leaving
                m_ServerWorld.EntityManager.AddComponentData(nceArray[i], new NetworkStreamRequestDisconnect());
            }
            //Then we dispose of our array
            nceArray.Dispose();
        }

#if UNITY_EDITOR
        if(Application.isPlaying)
#endif
            SceneManager.LoadSceneAsync("NavigationScene");
#if UNITY_EDITOR
        else
            Debug.Log("Loading: " + "NavigationScene");
#endif
        if (ClientServerInfo.IsServer)
                    m_ServerWorld.GetExistingSystem<GhostDistancePartitioningSystem>().Enabled = false;
    }

    //When the OnDestroy method is called (because of our transition to NavigationScene) we
    //must delete all our entities and our created worlds to go back to a blank state
    //This way we can move back and forth between scenes and "start from scratch" each time
    void OnDestroy()
    {
        for (var i = 0; i < launchObjects.Length; i++)
        {
            Destroy(launchObjects[i]);
        }
        foreach (var world in World.All)
        {
            var entityManager = world.EntityManager;
            var uq = entityManager.UniversalQuery;
            world.EntityManager.DestroyEntity(uq);
        }

        World.DisposeAllWorlds();

        //We return to our initial world that we started with, defaultWorld
        var bootstrap = new NetCodeBootstrap();
        bootstrap.Initialize("defaultWorld"); 

    }
}