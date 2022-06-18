using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;
using System.Collections;
using System;

public struct SendServerPlayerNameRpc : IRpcCommand
{
    public FixedString64Bytes playerName;
}