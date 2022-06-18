using Unity.Entities;
using Unity.Collections;

public struct GameNameComponent : IComponentData
{
    //Must used "FixedStringN" instead of stirng in IComponentData
    //This is a DOTS requirement because IComponentData must be a struct
    public FixedString64Bytes GameName;
}