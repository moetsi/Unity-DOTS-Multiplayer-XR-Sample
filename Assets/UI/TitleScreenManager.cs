using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleScreenManager : VisualElement
{
    VisualElement m_TitleScreen;
    VisualElement m_MoetsiScreen;
    VisualElement m_HostScreen;
    VisualElement m_JoinScreen;
    VisualElement m_ManualConnectScreen;
    
    public new class UxmlFactory : UxmlFactory<TitleScreenManager, UxmlTraits> { }

    public TitleScreenManager()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        m_MoetsiScreen = this.Q("MoetsiScreen");
        m_TitleScreen = this.Q("TitleScreen");
        m_HostScreen = this.Q("HostGameScreen");
        m_JoinScreen = this.Q("JoinGameScreen");
        m_ManualConnectScreen = this.Q("ManualConnectScreen");

        m_TitleScreen?.Q("join-moetsi-server")?.RegisterCallback<ClickEvent>(ev => EnableMoetsiScreen());
        m_TitleScreen?.Q("host-local-game")?.RegisterCallback<ClickEvent>(ev => EnableHostScreen());
        m_TitleScreen?.Q("join-local-game")?.RegisterCallback<ClickEvent>(ev => EnableJoinScreen());
        m_TitleScreen?.Q("manual-connect")?.RegisterCallback<ClickEvent>(ev => EnableManualScreen());

        m_MoetsiScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
        m_HostScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
        m_JoinScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
        m_ManualConnectScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());

        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    public void EnableMoetsiScreen()
    {
        m_MoetsiScreen.style.display = DisplayStyle.Flex;
        m_TitleScreen.style.display = DisplayStyle.None;
        m_HostScreen.style.display = DisplayStyle.None;
        m_JoinScreen.style.display = DisplayStyle.None;
        m_ManualConnectScreen.style.display = DisplayStyle.None;

    }

    public void EnableHostScreen()
    {
        m_MoetsiScreen.style.display = DisplayStyle.None;
        m_TitleScreen.style.display = DisplayStyle.None;
        m_HostScreen.style.display = DisplayStyle.Flex;
        m_JoinScreen.style.display = DisplayStyle.None;
        m_ManualConnectScreen.style.display = DisplayStyle.None;

    }

    public void EnableJoinScreen()
    {
        m_MoetsiScreen.style.display = DisplayStyle.None;
        m_TitleScreen.style.display = DisplayStyle.None;
        m_HostScreen.style.display = DisplayStyle.None;
        m_JoinScreen.style.display = DisplayStyle.Flex;
        m_ManualConnectScreen.style.display = DisplayStyle.None;
    }

    public void EnableManualScreen()
    {
        m_MoetsiScreen.style.display = DisplayStyle.None;
        m_TitleScreen.style.display = DisplayStyle.None;
        m_HostScreen.style.display = DisplayStyle.None;
        m_JoinScreen.style.display = DisplayStyle.None;
        m_ManualConnectScreen.style.display = DisplayStyle.Flex;
    }

    public void EnableTitleScreen()
    {
        m_MoetsiScreen.style.display = DisplayStyle.None;
        m_TitleScreen.style.display = DisplayStyle.Flex;
        m_HostScreen.style.display = DisplayStyle.None;
        m_JoinScreen.style.display = DisplayStyle.None;
        m_ManualConnectScreen.style.display = DisplayStyle.None;
    }

}
