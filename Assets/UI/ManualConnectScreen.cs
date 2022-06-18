using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class ManualConnectScreen : VisualElement
{
    TextField m_GameIp;
    TextField m_PlayerName;
    string m_HostName = "";
    IPAddress m_MyIp;

    public new class UxmlFactory : UxmlFactory<ManualConnectScreen, UxmlTraits> { }

    public ManualConnectScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        // 
        // PROVIDE ACCESS TO THE FORM ELEMENTS THROUGH VARIABLES
        // 
        m_GameIp = this.Q<TextField>("game-ip");
        m_PlayerName = this.Q<TextField>("player-name");

        //  CLICKING CALLBACKS
        this.Q("launch-connect-game")?.RegisterCallback<ClickEvent>(ev => ClickedJoinGame());

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void ClickedJoinGame()
    {
        Debug.Log("clicked manual connect");
    }

}
