using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.SceneManagement;

public class JoinGameScreen : VisualElement
{
    Label m_GameName;
    Label m_GameIp;
    TextField m_PlayerName;
    String m_HostName = "";
    IPAddress m_MyIp;

    public new class UxmlFactory : UxmlFactory<JoinGameScreen, UxmlTraits> { }

    public JoinGameScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        // 
        // PROVIDE ACCESS TO THE FORM ELEMENTS THROUGH VARIABLES
        // 
        m_GameName = this.Q<Label>("game-name");
        m_GameIp = this.Q<Label>("game-ip");
        m_PlayerName = this.Q<TextField>("player-name");

        //  CLICKING CALLBACKS
        this.Q("launch-host-game")?.RegisterCallback<ClickEvent>(ev => ClickedJoinGame());



        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void ClickedJoinGame()
    {
        Debug.Log("clicked client game");
    }


}