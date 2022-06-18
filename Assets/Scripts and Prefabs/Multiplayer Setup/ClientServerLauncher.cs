using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class ClientServerLauncher : MonoBehaviour
{
    //These will be used to grab the broadcasting port and address
    public LocalGamesFinder GameBroadcasting;
    private string m_BroadcastIpAddress;
    private ushort m_BroadcastPort;

    //These are the variables that will get us access to the UI views 
    //This is how we can grab active UI into a script
    //If this is confusing checkout the "Making a List" page in the gitbook
    
    //This is the UI Document from the Hierarchy in NavigationScene
    public UIDocument m_TitleUIDocument;
    private VisualElement m_titleScreenManagerVE;
    //These variables we will set by querying the parent UI Document
    private HostGameScreen m_HostGameScreen;
    private JoinGameScreen m_JoinGameScreen;
    private ManualConnectScreen m_ManualConnectScreen;

    //These will persist through the scene transition
    //MainScene will look for 1 or both of the objects
    //Based on what MainScene finds it will initialize as Server/Client
    public GameObject ServerLauncherObject;
    public GameObject ClientLauncherObject;

    //These pieces of data will be taken from the views
    //and put into the launch objects that persist between scenes
    public TextField m_GameName;
    public TextField m_GameIp;
    public Label m_GameIpLabel;
    public TextField m_PlayerName;


    void OnEnable()
    {

        //Here we set our variables for our different views so we can then add call backs to their buttons
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        m_HostGameScreen = m_titleScreenManagerVE.Q<HostGameScreen>("HostGameScreen");
        m_JoinGameScreen = m_titleScreenManagerVE.Q<JoinGameScreen>("JoinGameScreen");
        m_ManualConnectScreen = m_titleScreenManagerVE.Q<ManualConnectScreen>("ManualConnectScreen");

        //Host Game Screen callback
        m_HostGameScreen.Q("launch-host-game")?.RegisterCallback<ClickEvent>(ev => ClickedHostGame());
        //Join Game Screen callback
        m_JoinGameScreen.Q("launch-join-game")?.RegisterCallback<ClickEvent>(ev => ClickedJoinGame());
        //Manual Connect Screen callback
        m_ManualConnectScreen.Q("launch-connect-game")?.RegisterCallback<ClickEvent>(ev => ClickedConnectGame());
    }
    
    // Start is called before the first frame update
    void Start()
    {
        //We are grabbing the broadcasting information from the discover script
        //We are going to bundle it with the server launch object so it can broadcast at that information
        m_BroadcastIpAddress = GameBroadcasting.BroadcastIpAddress;
        m_BroadcastPort = GameBroadcasting.BroadcastPort;
    }

    void ClickedHostGame()
    {
        //This gets the latest values on the screen
        //Our HostGameScreen cVE defaults these values but player name and game name can be updated
        //We set these VisualElement variables OnClick instead of OnEnable because this way
        //we don't need to make a variable for player name for every view, just 1 and set which view
        //we get it from OnClick (which is when we need it)
        m_GameName = m_HostGameScreen.Q<TextField>("game-name");
        m_GameIpLabel = m_HostGameScreen.Q<Label>("game-ip");
        m_PlayerName = m_HostGameScreen.Q<TextField>("player-name");

        //Now we grab the values from the VisualElements
        var gameName = m_GameName.value;
        var gameIp = m_GameIpLabel.text;
        var playerName = m_PlayerName.value;

        //When we click "Host Game" that means we want to be both a server and a client
        //So we will trigger both functions for the server and client
        ServerLauncher(gameName);
        ClientLauncher(playerName, gameIp);

        //This function will trigger the MainScene
        StartGameScene();
    }

    void ClickedJoinGame()
    {
        //This gets the latest values on the screen
        //Our JoinGameScreen cVE defaults these values but player name can be updated
        //We set these VisualElement variables OnClick instead of OnEnable because this way
        //we don't need to make a variable for player name for every view, just 1 and set which view
        //we get it from OnClick (which is when we need it)
        m_GameIpLabel = m_JoinGameScreen.Q<Label>("game-ip");
        m_PlayerName = m_JoinGameScreen.Q<TextField>("player-name");

        //Now we grab the values from the VisualElements
        var gameIp = m_GameIpLabel.text;
        var playerName = m_PlayerName.value;

        //When we click "Join Game" that means we want to be only a client
        ClientLauncher(playerName, gameIp);

        //This function will trigger the MainScene
        StartGameScene();
    }

    void ClickedConnectGame()
    {
        //This gets the latest values on the screen
        //Our ManualConnectScreen cVE defaults these values but player name and IP address be updated
        //We set these VisualElement variables OnClick instead of OnEnable because this way
        //we don't need to make a variable for player name for every view, just 1 and set which view
        //we get it from OnClick (which is when we need it)
        m_GameIp = m_ManualConnectScreen.Q<TextField>("game-ip");
        m_PlayerName = m_ManualConnectScreen.Q<TextField>("player-name");

        //Now we grab the values from the VisualElements
        var gameIp = m_GameIp.value;
        var playerName = m_PlayerName.value;

        //When we click "Join Game" that means we want to be only a client
        ClientLauncher(playerName, gameIp);

        //This function will trigger the MainScene
        StartGameScene();
    }



    public void ServerLauncher(string gameName)
    {
        //Here we create the launch GameObject and load it with necessary data
        GameObject  serverObject = Instantiate(ServerLauncherObject);
        DontDestroyOnLoad(serverObject);

        //This sets up the server object with all its necessary data
        serverObject.GetComponent<ServerLaunchObjectData>().GameName = gameName;
        serverObject.GetComponent<ServerLaunchObjectData>().BroadcastIpAddress = m_BroadcastIpAddress;
        serverObject.GetComponent<ServerLaunchObjectData>().BroadcastPort = m_BroadcastPort;

        //CreateServerWorld is a method provided by ClientServerBootstrap for precisely this reason
        //Manual creation of worlds

        //We must grab the DefaultGameObjectInjectionWorld first as it is needed to create our ServerWorld
        var world = World.DefaultGameObjectInjectionWorld;
#if !UNITY_CLIENT || UNITY_SERVER || UNITY_EDITOR
        ClientServerBootstrap.CreateServerWorld(world, "ServerWorld");

#endif
    }

    public void ClientLauncher(string playerName, string ipAddress)
    {
        //Here we create the launch GameObject and load it with necessary data
        GameObject  clientObject = Instantiate(ClientLauncherObject);
        DontDestroyOnLoad(clientObject);
        clientObject.GetComponent<ClientLaunchObjectData>().PlayerName = playerName;
        clientObject.GetComponent<ClientLaunchObjectData>().IPAddress = ipAddress;

        //We grab the DefaultGameObjectInjectionWorld because it is needed to create ClientWorld
        var world = World.DefaultGameObjectInjectionWorld;

        //We have to account for the fact that we may be in the Editor and using ThinClients
        //We initially start with 1 client world which will not change if not in the editor
        int numClientWorlds = 1;
        int totalNumClients = numClientWorlds;

        //If in the editor we grab the amount of ThinClients from ClientServerBootstrap class (it is a static variable)
        //We add that to the total amount of worlds we must create
#if UNITY_EDITOR
        int numThinClients = ClientServerBootstrap.RequestedNumThinClients;
        totalNumClients += numThinClients;
#endif
        //We create the necessary number of worlds and append the number to the end
        for (int i = 0; i < numClientWorlds; ++i)
        {
            ClientServerBootstrap.CreateClientWorld(world, "ClientWorld" + i);
        }
#if UNITY_EDITOR
        for (int i = numClientWorlds; i < totalNumClients; ++i)
        {
            var clientWorld = ClientServerBootstrap.CreateClientWorld(world, "ClientWorld" + i);
            clientWorld.EntityManager.CreateEntity(typeof(ThinClientComponent));
        }
#endif
    }

    void StartGameScene()
    {
        //Here we trigger MainScene
#if UNITY_EDITOR
        if(Application.isPlaying)
#endif
            SceneManager.LoadSceneAsync("MainScene");
#if UNITY_EDITOR
        else
            Debug.Log("Loading: " + "MainScene");
#endif
    }
}