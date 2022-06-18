using Unity.Entities;
using Unity.Collections;


 public struct ServerDataComponent : IComponentData
{
    public FixedString64Bytes GameName;
    public ushort GamePort;
}