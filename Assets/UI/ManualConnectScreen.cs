using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class ManualConnectScreen : VisualElement
{
    //We will update these fields with system data
    TextField m_GameIp;
    TextField m_PlayerName;

    //These are the system data variables we will be using
    string m_HostName = "";

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

        // 
        // INITIALIZE ALL THE TEXT FIELD WITH NETWORK INFORMATION
        // 
        m_HostName = Dns.GetHostName();

        //Now we set our VisualElement fields
        m_PlayerName.value = m_HostName;

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }
}