using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

public class LocalGamesFinder : MonoBehaviour
{
    //We will be pulling in our SourceAsset from TitleScreenUI GameObject so we can reference Visual Elements
    public UIDocument m_TitleUIDocument;

    //When we grab the rootVisualElement of our UIDocument we will be able to query the TitleScreenManager Visual Element
    private VisualElement m_titleScreenManagerVE;

    //We will query for our TitleScreenManager cVE by its name "TitleScreenManager"
    private TitleScreenManager m_titleScreenManagerClass;

    //Within TitleScreenManager (which is everything) we will query for our list-view by name
    //We don't have to query for the TitleScreen THEN list-view because it is one big tree of elements
    //We can call any child from the parent, very convenient! But you must be mindful about being dilligent about
    //creating unique names or else you can get back several elements (which at times is the point of sharing a name)
    private ListView m_ListView;

    //This is where we will store our received broadcast messages
    private List<ServerInfoObject> discoveredServerInfoObjects = new List<ServerInfoObject>();

    //This is our ListItem uxml that we will drag to the public field
    //We need a reference to the uxml so we can build it in makeItem
    public VisualTreeAsset m_localGameListItemAsset;

    //These variables are used in Update() to pace how often we check for GameObjects
    public float perSecond = 1.0f;
    private float nextTime = 0; 

    ///The broadcast ip address and port to be used by the server across the LAN
    public string BroadcastIpAddress = "192.168.1.255";
    public ushort BroadcastPort = 8014;

    //We will be storing our UdpConnection class as connection
    private UdpConnection connection;

    void OnEnable()
    {
        //Here we grab the SourceAsset rootVisualElement
        //This is a MAJOR KEY, really couldn't find this key step in information online
        //If you want to reference your active UI in a script make a public UIDocument variable and 
        //then call rootVisualElement on it, from there you can query the Visual Element tree by names
        //or element types
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        //Here we grab the TitleScreenManager by querying by name
        m_titleScreenManagerClass = m_titleScreenManagerVE.Q<TitleScreenManager>("TitleScreenManager");
        //From within TitleScreenManager we query local-games-list by name
        m_ListView = m_titleScreenManagerVE.Q<ListView>("local-games-list");

    }

    // Start is called before the first frame update
    void Start()
    {
        //First we pull our broadcast IP address and port from our source of truth, which is this component
        //We actually don't need the broadcast IP address when listening but we provide it anyway because the method
        //requires both arguments (we could provide any IP address and it wouldn't matter, only the server needs to provide the right one)
        string broadcastIp = BroadcastIpAddress;
        //This is the port we will be listening on (this has to be the same as the port the server is sending on)
        int receivePort = BroadcastPort;
 
        //Next we create our UdpConnection class
        connection = new UdpConnection();
        //We provide our broadcast IP adress and port
        connection.StartConnection(broadcastIp, receivePort);
        //Then we start our receive thread which means "listen"
        //This will start creating our queue of received messages that we will call in update
        connection.StartReceiveThread();
        
        
        // The three spells you must cast to conjure a list view
        m_ListView.makeItem = MakeItem;
        m_ListView.bindItem = BindItem;
        m_ListView.itemsSource = discoveredServerInfoObjects;

    }

    private VisualElement MakeItem()
    {
        //Here we take the uxml and make a VisualElement
        VisualElement listItem = m_localGameListItemAsset.CloneTree();
        return listItem;

    }

    private void BindItem(VisualElement e, int index)
    {
        //We add the game name to the label of the list item
        e.Q<Label>("game-name").text = discoveredServerInfoObjects[index].gameName;

        //Here we create a call back for clicking on the list item and provide data to a function
        e.Q<Button>("join-local-game").RegisterCallback<ClickEvent>(ev => ClickedJoinGame(discoveredServerInfoObjects[index]));

    }

    void ClickedJoinGame(ServerInfoObject localGame)
    {
        //We query our JoinGameScreen cVE and call a new function LoadJoinScreenForSelectedServer and pass our GameObject
        //This is an example of clicking a list item and passing through data to a new function with that click
        //You will see in our JoinGameScreen cVE that we use this data to fill labels in the view
        m_titleScreenManagerClass.Q<JoinGameScreen>("JoinGameScreen").LoadJoinScreenForSelectedServer(localGame);

        //We then call EnableJoinScreen on our TitleScreenManager cVE (which displays JoinGameScreen)
        m_titleScreenManagerClass.EnableJoinScreen();

    }
  
    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextTime)
        {   
            //We grab our array of ServerInfoObjects from our UdpConnection class
            foreach (ServerInfoObject serverInfo in connection.getMessages())
            {
                //We call ReceivedServerInfo so we can check if this ServerInfoObject contains new information
                //We don't use it immediatly and add it to our list because it might already be in the list
                ReceivedServerInfo(serverInfo);
            }
            //We increment
            nextTime += (1/perSecond);
        }
    }

    void ReceivedServerInfo(ServerInfoObject serverInfo)
    {
        //Filter to see if this ServerInfoObject matches with previous broadcasts
        //We will start by thinking that it does not exist
        bool ipExists = false;

        foreach (ServerInfoObject discoveredInfo in discoveredServerInfoObjects)
        {
            //Check if this discovered ip address is already known
            if (serverInfo.ipAddress == discoveredInfo.ipAddress)
            {
                ipExists = true;

                //If a ServerInfoObject with this IP address has been discovered, when did we hear about it?
                float receivedTime = float.Parse(serverInfo.timeStamp);
                //What about this broadcast?
                float storedTime = float.Parse(discoveredInfo.timeStamp);

                //We will update to the latest information from the IP address that has been broadcast
                //The host might have quit and started a new game and we want to display the latest info
                if (receivedTime > storedTime)
                {
                    //Set the data to the new data
                    discoveredInfo.gameName = serverInfo.gameName;
                    discoveredInfo.timeStamp = serverInfo.timeStamp;
                    
                    //Now we need to update the table
                    m_ListView.Refresh();
                }
            }

        }
        //If the ip didn't already exist, add it to the known list
        if (!ipExists)
        {
            //We add it to the list
            discoveredServerInfoObjects.Add(serverInfo);
            //We refresh our list to display the new data
            m_ListView.Refresh();
        }
    }

    //We must call the clean up function on UdpConnection or else the thread will keep running!
    void OnDestroy()
    {
        connection.Stop();
    }
}