using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ServerOnlyLauncher : MonoBehaviour
{
    public ClientServerLauncher m_ClientServerLauncher;
    //Moetsi Server IP
    public string m_GameName = "Moetsi's Dedicated Server";

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 120;

#if !UNITY_CLIENT || UNITY_SERVER || !UNITY_EDITOR
        m_ClientServerLauncher.ServerLauncher(m_GameName);
        m_ClientServerLauncher.StartGameScene();
#endif

    }
}