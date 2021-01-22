using System;
using Unity.Entities;
using Unity.Collections;

public struct ClientDataComponent : IComponentData
{
    //Must used "FixedStringN" instead of stirng in IComponentData
    //This is a DOTS requirement because IComponentData must be a struct
    public FixedString64 ConnectToServerIp;
    public ushort GamePort;
    public FixedString64 PlayerName;
}
