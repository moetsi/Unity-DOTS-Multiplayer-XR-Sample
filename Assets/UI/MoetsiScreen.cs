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

public class MoetsiScreen : VisualElement
{
    TextField m_PlayerName;
    String m_HostName = "";

    public new class UxmlFactory : UxmlFactory<MoetsiScreen, UxmlTraits> { }

    public MoetsiScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        // 
        // PROVIDE ACCESS TO THE FORM ELEMENTS THROUGH VARIABLES
        // 
        m_PlayerName = this.Q<TextField>("player-name");

        //Grab the system name
        m_HostName = Dns.GetHostName();
        //Set the value equal to the host name to start
        m_PlayerName.value = m_HostName;

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }
}