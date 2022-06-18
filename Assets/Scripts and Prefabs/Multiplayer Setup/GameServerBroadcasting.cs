using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

public class GameServerBroadcasting : MonoBehaviour
{
    //We will be using our UdpConnection class to send messages
    private UdpConnection connection;

    //This will decide how often we send a broadcast messages
    //We found that sending a broadcast message once every 2 seconds worked well
    //you don't want to flood the network with broadcast packets
    public float perSecond = .5f;
    private float nextTime = 0;

    //We will pull in game and broadcast information through ClientServerInfo
    public ClientServerInfo ClientServerInfo;
 
    void Start()
    {
        //We only need to run this if we are the server, otherwise disable
        if (!ClientServerInfo.IsServer)
        {
            this.enabled = false;         
        }

        //Get the broadcasting address and port from ClientServerInfo
        string sendToIp = ClientServerInfo.BroadcastIpAddress;
        int sendToPort = ClientServerInfo.BroadcastPort;
 
        //First we create our class
        connection = new UdpConnection();
        //Then we run the initialization method and provide the broadcast IP address and port
        connection.StartConnection(sendToIp, sendToPort);
    }
 
    void Update()
    {
        //We check if it is time to send another broadcast
        if (Time.time >= nextTime)
        {
            //If it is we provide the Send method the game name and time
            //These will be bundled with the server's IP address (which is generated in StartConnection)
            //to be included in the broadcast packet
            connection.Send(nextTime, ClientServerInfo.GameName);
            nextTime += (1/perSecond);
        }
    }

    void OnDestroy()
    {
        //If the server destroys this scene (by returning to NavigationScene) we will call the clean up method
        connection.Stop();
    }
}