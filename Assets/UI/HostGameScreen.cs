using UnityEngine;
using UnityEngine.UIElements;

public class HostGameScreen : VisualElement
{
    
    public new class UxmlFactory : UxmlFactory<HostGameScreen, UxmlTraits> { }

    public HostGameScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }
}