using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
#if UNITY_EDITOR
using Unity.NetCode.Editor;
#endif

public class NetCodeBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {

        var world = new World(defaultWorldName);
        World.DefaultGameObjectInjectionWorld = world;

        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        GenerateSystemLists(systems);

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, ExplicitDefaultWorldSystems);
#if !UNITY_DOTSRUNTIME
        ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);
#endif
        return true;
    }
}