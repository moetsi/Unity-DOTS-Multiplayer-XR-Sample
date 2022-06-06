using Unity.Entities;

[GenerateAuthoringComponent]
public struct ClientSettings : IComponentData
{
    public float predictionRadius;
    public float predictionRadiusMargin;

}