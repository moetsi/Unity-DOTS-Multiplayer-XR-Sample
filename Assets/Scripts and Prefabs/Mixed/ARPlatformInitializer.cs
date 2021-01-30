using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Entities;
using Unity.NetCode;

public class ARPlatformInitializer : MonoBehaviour
{
    [SerializeField] GameObject m_Session;
    [SerializeField] GameObject m_SessionOrigin;

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

        }
    }
}