using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.UIElements;

public class ARPlatformInitializer : MonoBehaviour
{
    [SerializeField] GameObject m_Session;
    [SerializeField] GameObject m_SessionOrigin;

    //This is how we will grab access to the UI elements we need to update
    public UIDocument m_GameUIDocument;
    private VisualElement m_GameManagerUIVE;
    private VisualElement m_BottomLeft;
    private Label m_1stInstruction;
    private Label m_2ndInstruction;
    private Label m_3rdInstruction;
    private Label m_4thInstruction;

    void OnEnable()
    {
        //We set the labels that we will need to update
        m_GameManagerUIVE = m_GameUIDocument.rootVisualElement;
        m_BottomLeft = m_GameManagerUIVE.Q<VisualElement>("bottom-left");
        m_4thInstruction = m_GameManagerUIVE.Q<Label>("instructions-4");
        m_3rdInstruction = m_GameManagerUIVE.Q<Label>("instructions-3");
        m_2ndInstruction = m_GameManagerUIVE.Q<Label>("instructions-2");
        m_1stInstruction = m_GameManagerUIVE.Q<Label>("instructions-1");
    }

    IEnumerator Start() {
        if ((ARSession.state == ARSessionState.None) ||
            (ARSession.state == ARSessionState.CheckingAvailability))
        {
            yield return ARSession.CheckAvailability();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            //If we AR is unsupported we disable both GameObjects
            m_SessionOrigin.SetActive(false);
            m_Session.SetActive(false);
        }
        else
        {
            //If AR is supported we create our IsARPlayerComponent singleton in ClientWorld
            foreach (var world in World.All)
            {
                if (world.GetExistingSystem<ClientSimulationSystemGroup>() != null)
                {
                    world.EntityManager.CreateEntity(typeof(IsARPlayerComponent));
                }
            }

            //Due to a UI Toolkit bug we cannot currently update existing labels without getting wacky behavior
            // https://forum.unity.com/threads/updating-labels-causes-a-gap-on-first-ui-view.1049081/
            //So we will remove all labels and attach new ones to our bottom left container
            m_BottomLeft.Remove(m_4thInstruction);
            m_BottomLeft.Remove(m_3rdInstruction);
            m_BottomLeft.Remove(m_2ndInstruction);
            m_BottomLeft.Remove(m_1stInstruction);

            //Now that our container is empty we make 3 new labels for our 3 new instructions
            Label instruction1 = new Label();
            Label instruction2 = new Label();
            Label instruction3 = new Label();

            //Now we add our instruction-text class to our labels so they have the same styling as before
            instruction1.AddToClassList("instruction-text");
            instruction2.AddToClassList("instruction-text");
            instruction3.AddToClassList("instruction-text");

            //Now we update the instruction text in the labels
            instruction3.text = "Tap with 1 finger to spawn and shoot";
            instruction2.text = "Move device to move player";
            instruction1.text = "Tap with 3 fingers to self-destruct";

            //Because we flex grow upwards we start with the bottom instruction (instruction 1) and then add the rest
            m_BottomLeft.Add(instruction1);
            m_BottomLeft.Add(instruction2);
            m_BottomLeft.Add(instruction3);
        }
    }
}