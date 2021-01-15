using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class ClientServerConnectionHandler : MonoBehaviour
{
    //this is the store of server/client info
    public ClientServerInfo ClientServerInfo;

    // these are the launch objects from Navigation scene that tells what to set up
    private GameObject[] launchObjects;

    //these will gets access to the UI views 
    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;

    void OnEnable()
    {
        // This will put callback on "Quit Game" button
        // This triggers the clean up function (ClickedQuitGame)
        m_GameManagerUIVE = m_GameUIDocument.rootVisualElement;
        m_GameManagerUIVE.Q("quit-game")?.RegisterCallback<ClickEvent>(ev => ClickedQuitGame());
    }

    void Awake()
    {
        launchObjects = GameObject.FindGameObjectsWithTag("LaunchObject");
        foreach(GameObject launchObject in launchObjects)
        {
            ///  
            // checks for server launch object
            // does set up for the server for listening to connections and player scores
            //
            if(launchObject.GetComponent<ServerLaunchObjectData>() != null)
            {
                //sets the gameobject server data (mono)
                ClientServerInfo.IsServer = true;
                
                //sets the component server data in server world(dots)
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
                            GamePort = ClientServerInfo.GamePort
                        });
                        //create component that allows server initialization to run
                        world.EntityManager.CreateEntity(typeof(InitializeServerComponent));
                    }
                }
            }

            // 
            // checks for client launch object
            //  does set up for client for dots and mono
            // 
            if(launchObject.GetComponent<ClientLaunchObjectData>() != null)
            {
                //sets the gameobject data in ClientServerInfo (mono)
                //sets the gameobject data in ClientServerInfo (mono)
                ClientServerInfo.IsClient = true;
                ClientServerInfo.ConnectToServerIp = launchObject.GetComponent<ClientLaunchObjectData>().IPAddress;                

                //sets the component client data in server world(dots)
                //ClientServerConnectionControl (client) will run in client world
                //it will pick up this component and use it connect to IP and port
                foreach (var world in World.All)
                {
                    //we cycle through all the worlds, and if the world has ClientSimulationSystemGroup
                    //we move forward (because that is the client world)
                    if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                    {
                        var ClientDataEntity = world.EntityManager.CreateEntity();
                        world.EntityManager.AddComponentData(ClientDataEntity, new ClientDataComponent
                        {
                            ConnectToServerIp = ClientServerInfo.ConnectToServerIp,
                            GamePort = ClientServerInfo.GamePort
                        });
                        //create component that allows client initialization to run
                        world.EntityManager.CreateEntity(typeof(InitializeClientComponent));
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
        
    }
   //This function will navigate us to NavigationScene
    void ClickedQuitGame()
    {

#if UNITY_EDITOR
        if(Application.isPlaying)
#endif
            SceneManager.LoadSceneAsync("NavigationScene");
#if UNITY_EDITOR
        else
            Debug.Log("Loading: " + "NavigationScene");
#endif
    }

    //When the OnDestroy method is called (because of our transition to NavigationScene) we
    //must delete all our entities and our created worlds to go back to a blank state
    //This way we can move back and forth between scenes and "start from scratch" each time
    void OnDestroy()
    {
        //This query deletes all entities
        World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery);
        //This query deletes all worlds
        World.DisposeAllWorlds();

        //We return to our initial world that we started with, defaultWorld
        var bootstrap = new NetCodeBootstrap();
        bootstrap.Initialize("defaultWorld"); 

    }
}