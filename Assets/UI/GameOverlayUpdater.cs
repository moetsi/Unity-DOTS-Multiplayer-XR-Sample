using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Jobs;

public class GameOverlayUpdater : MonoBehaviour
{
    //This is how we will grab access to the UI elements we need to update
    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;
    private Label m_GameName;
    private Label m_GameIp;
    private Label m_PlayerName;
    private Label m_CurrentScoreText;
    private Label m_HighScoreText;
    private Label m_HighestScoreText;
    
    //We will need ClientServerInfo to update our VisualElements with appropriate valuess
    public ClientServerInfo ClientServerInfo;
    private ClientSimulationSystemGroup m_ClientWorldSimulationSystemGroup;

    //Will check for GameNameComponent
    private EntityQuery m_GameNameComponentQuery;
    private bool gameNameIsSet = false;

    void OnEnable()
    {

        //We set the labels that we will need to update
        m_GameManagerUIVE = m_GameUIDocument.rootVisualElement;
        m_GameName = m_GameManagerUIVE.Q<Label>("game-name");
        m_GameIp = m_GameManagerUIVE.Q<Label>("game-ip");
        m_PlayerName = m_GameManagerUIVE.Q<Label>("player-name");

        //Scores will be updated in a future section
        m_CurrentScoreText = m_GameManagerUIVE.Q<Label>("current-score");
        m_HighScoreText = m_GameManagerUIVE.Q<Label>("high-score");
        m_HighestScoreText = m_GameManagerUIVE.Q<Label>("highest-score");
    }

    // Start is called before the first frame update
    void Start()
    {
        //We set the initial client data we already have as part of ClientDataComponent
        m_GameIp.text = ClientServerInfo.ConnectToServerIp;
        m_PlayerName.text = ClientServerInfo.PlayerName;
        
        //If it is not the client, stop running this script (unnecessary)
        if (!ClientServerInfo.IsClient)
        {
            this.enabled = false;         
        }
        
        //Now we search for the client world and the client simulation system group
        //so we can communicated with ECS in this MonoBehaviour
        foreach (var world in World.All)
        {
            if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
            {
                m_ClientWorldSimulationSystemGroup = world.GetExistingSystem<ClientSimulationSystemGroup>();
                m_GameNameComponentQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GameNameComponent>());
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        //We do not need to continue if we do not have a GameNameComponent yet
        if(m_GameNameComponentQuery.IsEmptyIgnoreFilter)
            return;

        //If we have a GameNameComponent we need to update ClientServerInfo and then our UI
        //We only need to do this once so we have a boolean flag to prevent this from being ran more than once
        if(!gameNameIsSet)
        {
                ClientServerInfo.GameName = m_ClientWorldSimulationSystemGroup.GetSingleton<GameNameComponent>().GameName.ToString();
                m_GameName.text = ClientServerInfo.GameName;
                gameNameIsSet = true;
        }
    }
}