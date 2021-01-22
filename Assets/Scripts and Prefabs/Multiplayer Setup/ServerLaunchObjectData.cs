using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class ServerLaunchObjectData : MonoBehaviour
{
    //This will be set by ClientServerLauncher in NavigationScene
    //It will then be pulled out in MainScene and put into ClientServerInfo
    public string GameName;
    public string BroadcastIpAddress;
    public ushort BroadcastPort;    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}