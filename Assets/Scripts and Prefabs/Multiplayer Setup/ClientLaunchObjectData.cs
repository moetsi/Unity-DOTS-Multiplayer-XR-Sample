using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class ClientLaunchObjectData : MonoBehaviour
{
    //This will be set by ClientServerLauncher in NavigationScene
    //It will then be pulled out in MainScene and put into ClientServerInfo
    public string PlayerName;
    public string IPAddress;
}