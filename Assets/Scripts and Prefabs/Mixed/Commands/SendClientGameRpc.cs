using AOT;
using Unity.Burst;
using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;
using System.Collections;
using System;

public struct SendClientGameRpc : IRpcCommand
{
    public int levelWidth;
    public int levelHeight;
    public int levelDepth;
    public float playerForce;
    public float bulletVelocity;
}