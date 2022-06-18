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

public class HostGameScreen : VisualElement
{
    TextField m_GameName;
    TextField m_GameIp;
    TextField m_PlayerName;
    String m_HostName = "";
    IPAddress m_MyIp;

    public new class UxmlFactory : UxmlFactory<HostGameScreen, UxmlTraits> { }

    public HostGameScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        // 
        // PROVIDE ACCESS TO THE FORM ELEMENTS THROUGH VARIABLES
        // 
        m_GameName = this.Q<TextField>("game-name");
        m_GameIp = this.Q<TextField>("game-ip");
        m_PlayerName = this.Q<TextField>("player-name");

        //  CLICKING CALLBACKS
        this.Q("launch-host-game")?.RegisterCallback<ClickEvent>(ev => ClickedHostGame());


        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void ClickedHostGame()
    {

        Debug.Log("clicked host game");
    }

}
