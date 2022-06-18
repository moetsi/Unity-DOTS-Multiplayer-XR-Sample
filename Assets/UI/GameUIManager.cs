using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.NetCode;

public class GameUIManager : VisualElement
{
    VisualElement m_LeaveArea;

    public new class UxmlFactory : UxmlFactory<GameUIManager, UxmlTraits> { }

    public GameUIManager()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        m_LeaveArea = this.Q("quit-game");

        m_LeaveArea?.RegisterCallback<ClickEvent>(ev => ClickedButton());
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void  ClickedButton() {

        Debug.Log("Clicked quit game");
    }  
}