using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

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

    //We need the PlayerScores and HighestScore as well as our NetworkId
    //We are going to set our NetworkId and then query the ghosts for the PlayerScore entity associated with us
    private EntityQuery m_NetworkConnectionEntityQuery;
    private EntityQuery m_PlayerScoresQuery;
    private EntityQuery m_HighestScoreQuery;
    private bool networkIdIsSet = false;
    private int m_NetworkId;
    private Entity ClientPlayerScoreEntity;
    public int m_CurrentScore;
    public int m_HighScore;
    public int m_HighestScore;
    public string m_HighestScoreName;

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

                //Grabbing the queries we need for updating scores
                m_NetworkConnectionEntityQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkIdComponent>());
                m_PlayerScoresQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerScoreComponent>());
                m_HighestScoreQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HighestScoreComponent>());
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

        //Now we will handle updating scoring
        //We check if the scoring entities exist, otherwise why bother
        if(m_NetworkConnectionEntityQuery.IsEmptyIgnoreFilter || m_PlayerScoresQuery.IsEmptyIgnoreFilter || m_HighestScoreQuery.IsEmptyIgnoreFilter)
            return;

        //We set our NetworkId once
        if(!networkIdIsSet)
        {

            m_NetworkId = m_ClientWorldSimulationSystemGroup.GetSingleton<NetworkIdComponent>().Value;
            networkIdIsSet = true;
        }

        //We set our PlayerScore entity once
        if (ClientPlayerScoreEntity == Entity.Null)
        {
            //Grab PlayerScore entities
            var playerScoresNative = m_PlayerScoresQuery.ToEntityArray(Allocator.TempJob);

            //For each entity find the entity with a matching NetworkId
            for (int j = 0; j < playerScoresNative.Length; j++)
            {
                //Grab the NetworkId of the PlayerScore entity
                var netId = m_ClientWorldSimulationSystemGroup.GetComponentDataFromEntity<PlayerScoreComponent>(true)[playerScoresNative[j]].networkId;
                //Check if it matches our NetworkId that we set
                if(netId == m_NetworkId)
                {
                    //If it matches set our ClientPlayerScoreEntity
                    ClientPlayerScoreEntity = playerScoresNative[j];
                }
            }
            //No need for this anymore
            playerScoresNative.Dispose();
        }
        
        //Every Update() we get grab the PlayerScoreComponent from our set Entity and check it out with current values
        var playerScoreComponent = m_ClientWorldSimulationSystemGroup.GetComponentDataFromEntity<PlayerScoreComponent>(true)[ClientPlayerScoreEntity];
        
        //Check if current is different and update to ghost value
        if(m_CurrentScore != playerScoreComponent.currentScore)
        {
            //If it is make it match the ghost value
            m_CurrentScore = playerScoreComponent.currentScore;
            UpdateCurrentScore();
        }
        //Check if current is different and update to ghost value
        if(m_HighScore != playerScoreComponent.highScore)
        {
            //If it is make it match the ghost value
            m_HighScore = playerScoreComponent.highScore;
            UpdateHighScore();
        }

        //We grab our HighestScoreComponent
        var highestScoreNative = m_HighestScoreQuery.ToComponentDataArray<HighestScoreComponent>(Allocator.TempJob);

        //We check if its current  value is different than ghost value
        if(highestScoreNative[0].highestScore != m_HighestScore)
        {
            //If it is make it match the ghost value
            m_HighestScore = highestScoreNative[0].highestScore;
            m_HighestScoreName = highestScoreNative[0].playerName.ToString();
            UpdateHighestScore();
        }
        highestScoreNative.Dispose();
    }
    void UpdateCurrentScore()
    {
        m_CurrentScoreText.text = m_CurrentScore.ToString();
    }
    void UpdateHighScore()
    {
        m_HighScoreText.text = m_HighScore.ToString();
    }
    void UpdateHighestScore()
    {
        m_HighestScoreText.text = m_HighestScoreName.ToString() + " - " + m_HighestScore.ToString();
    }
}