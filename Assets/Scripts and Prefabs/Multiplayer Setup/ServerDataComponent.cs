using Unity.Entities;
using Unity.Collections;


 public struct ServerDataComponent : IComponentData
{
    public FixedString64 GameName;
    public ushort GamePort;
}