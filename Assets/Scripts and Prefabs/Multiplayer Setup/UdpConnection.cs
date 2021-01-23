using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;


// 
// "silent hero award" for this implementation
// https://forum.unity.com/threads/simple-udp-implementation-send-read-via-mono-c.15900/#post-3645256
// 
public class UdpConnection
{
    //In this variable we will store our actual udpClient object (from Microsoft)
    private UdpClient udpClient;

    //This is the broadcast address that we will be passed from the server for where to send our messages
    private string sendToIp;
    //This is the broadcast port, we either bind to it and listen on it as a client
    //or we bind to it and SEND to it as the server
    //It actually doesn't matter whhat port we bind on as the server, as long as we send to this port
    //but we decided to just bind to it as well to keep track of less numbers (it does mean we need to do)
    private int sendOrReceivePort;
 
    //This is a Queue of our messages (where we store received broadcast messages)
    private readonly Queue<string> incomingQueue = new Queue<string>();
    //This is the thread we will start to "listen" on
    Thread receiveThread;
    //We need to know if we were listening on a thread so we know whether to turn it off when we don't need it
    //If we are the server this will stay false
    private bool threadRunning = false;
    //The server will need to find its IP address so it can send it out to clients
    private IPAddress m_MyIp;

    //We call this method as a way to initialize our UdpConnection
    //We pass through the broadcast address (sendToIp) and the broadcast port (sendOrReceivePort)
    public void StartConnection(string sendToIp, int sendOrReceivePort)
    {
        //We create our udpClient by binding it to the sendOrReceivePort
        //Binding to the broadcast port really only matters if you are a client listening for our broadcast messages
        //The server could actually bind to any port
        try { udpClient = new UdpClient(sendOrReceivePort); }
        catch (Exception e)
        {
            Debug.Log("Failed to listen for UDP at port " + sendOrReceivePort + ": " + e.Message);
            return;
        }
        // "best tip of all time award" to MichaelBluestein
        // https://forums.xamarin.com/discussion/comment/1206/#Comment_1206
        // somehow you get IP address
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces()) {
            if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses) {
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork) {

                        //We will use this address to broadcast out to clients as the server
                        m_MyIp = addrInfo.Address;
                    }
                }
            }  
        }
        //Now we configure our udpClient to be able to broadcast
        udpClient.EnableBroadcast = true;

        //We set our broadcast IP and broadcast port
        this.sendToIp = sendToIp;
        this.sendOrReceivePort = sendOrReceivePort;
    }
 
    //This will only be called by the client in order to start "listening"
    public void StartReceiveThread()
    {
        //We create our new thread that be running the method "ListenForMessages"
        receiveThread = new Thread(() => ListenForMessages(udpClient));
        //We configure the thread we just created
        receiveThread.IsBackground = true;
        //We note that it is running so we don't forget to turn it off
        threadRunning = true;
        //Now we start the thread
        receiveThread.Start();
    }
 
    //This method is called by StartReceiveThread()
    private void ListenForMessages(UdpClient client)
    {
        //We create our listening endpoint
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
 
        //We will continue running this until we turn "threadRunning" to false (which is when we don't need to listen anymore)
        while (threadRunning)
        {
            try
            {
                //A little console log to know we have started listening
                Debug.Log("starting receive on " + m_MyIp.ToString() +" and port " +sendOrReceivePort.ToString());
                
                // Blocks until a message returns on this socket from a remote host.
                Byte[] receiveBytes = client.Receive(ref remoteIpEndPoint);
                //We grab our byte stream as UTF8 encoding
                string returnData = Encoding.UTF8.GetString(receiveBytes);
                
                //We enqueue our received byte stream 
                lock (incomingQueue)
                {
                    incomingQueue.Enqueue(returnData);
                }
            }
            //Error handling
            catch (SocketException e)
            {
                // 10004 thrown when socket is closed
                if (e.ErrorCode != 10004) Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
            }
            catch (Exception e)
            {
                Debug.Log("Error receiving data from udp client: " + e.Message);
            }

            //We take a pause after receiving a message and run it again
            Thread.Sleep(1);
        }
    }
 
    //This is another method the client will call to grab all the messages that have been received by listening
    public ServerInfoObject[] getMessages()
    {
        //We created an array of pending messages
        string[] pendingMessages = new string[0];
        //We create an array where we will store our ServerInfoObjects (how we store the JSON)
        ServerInfoObject[] pendingServerInfos = new ServerInfoObject[0];
        //While we get this done we need to lock the Queue
        lock (incomingQueue)
        {
            //We set our pending messages the length of our queue of byte stream
            pendingMessages = new string[incomingQueue.Count];
            //We set our pending server infos the length of our queue of byte stream
            pendingServerInfos = new ServerInfoObject[incomingQueue.Count];
            
            //We will go through all our messages and update to get an array of ServerInfoObjects
            int i = 0;
            while (incomingQueue.Count != 0)
            {
                //We start moving data from the queue to our pending messages
                pendingMessages[i] = incomingQueue.Dequeue();
                //We then take our pending message
                string jsonObject = pendingMessages[i];
                //And use FromJson to create our array of ServerInfoObjects
                pendingServerInfos[i] = JsonUtility.FromJson<ServerInfoObject>(jsonObject);
                i++;
            }
        }
        //We return an array of ServerInfoObjects to the client that called this method
        return pendingServerInfos;
    }

    //We will only call this method on the server and provide it the game name and time
    //We don't provide the game name in StartConnection because the client won't have it and 
    //both the client and server use StartConnection
    public void Send(float floatTime, string gameName)
    {
        //All our values need to be string to be able to be serialized
        string stringTime = floatTime.ToString();

        //We need to create our destination endpoint
        //It will be at the provided broadcast IP address and port
        IPEndPoint sendToEndpoint = new IPEndPoint(IPAddress.Parse(sendToIp), sendOrReceivePort);

        //We create a new ServerInfoObject which we will use to store our data
        ServerInfoObject thisServerInfoObject = new ServerInfoObject();
        //We populate our ServerInfoObject with data
        thisServerInfoObject.gameName = gameName;
        thisServerInfoObject.ipAddress = m_MyIp.ToString();
        thisServerInfoObject.timeStamp = stringTime;

        //Then we turn it into JSON
        string json = JsonUtility.ToJson(thisServerInfoObject);
        //Then we create a sendBytes array the size of the bytes of the JSON
        Byte[] sendBytes = Encoding.UTF8.GetBytes(json);
        //We then call send method on udpClient to send our byte array
        udpClient.Send(sendBytes, sendBytes.Length, sendToEndpoint);
    }
 
    public void Stop()
    {
        // Not always UdpClients are used for listening
        // Which is what requires the running thread to listen
        if (threadRunning == true)
        {
            threadRunning = false;
            receiveThread.Abort();
        }
        udpClient.Close();
        udpClient.Dispose();
    }
}

//This is our ServerInfoObject that we will be using to help us send data as JSON
[Serializable]
public class ServerInfoObject
{
    public string gameName = "";
    public string ipAddress = "";
    public string timeStamp = "";

}