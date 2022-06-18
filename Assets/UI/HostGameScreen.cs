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
    //We will update these fields with system data
    TextField m_GameName;
    Label m_GameIp;
    TextField m_PlayerName;

    //These are the system data variables we will be using
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
        m_GameIp = this.Q<Label>("game-ip");
        m_PlayerName = this.Q<TextField>("player-name");

        // 
        // INITIALIZE ALL THE TEXT FIELD WITH NETWORK INFORMATION
        //        
        m_HostName = Dns.GetHostName();
        // "best tip of all time award" to MichaelBluestein
        // somehow this is the best way to get your IP address on all the internet
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (netInterface.OperationalStatus == OperationalStatus.Up &&  
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses) {
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork) {

                        m_MyIp = addrInfo.Address;
                    }
                }
            }  
        }

        //Now we set our VisualElement fields
        m_GameName.value = m_HostName;
        m_GameIp.text = m_MyIp.ToString();
        m_PlayerName.value = m_HostName;

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }
}
