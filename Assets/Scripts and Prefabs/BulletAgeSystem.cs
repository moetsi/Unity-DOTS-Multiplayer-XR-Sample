using Unity.Entities;

public partial class BulletAgeSystem : SystemBase
{
    //We will be using the BeginSimulationEntityCommandBuffer to record our structural changes
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        //We create our CommandBuffer and add .AsParallelWriter() because we will be scheduling parallel jobs
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer().AsParallelWriter();

        //We must declare local variables before using them in the job below
        var deltaTime = Time.DeltaTime;

        //Our query writes to the BulletAgeComponent
        //The reason we don't need to add .WithAll<BulletTag>() here is because referencing the BulletAgeComponent
        //requires the Entities to have a BulletAgeComponent and only Bullets have those
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref BulletAgeComponent age) =>
        {
            age.age += deltaTime;
            if (age.age > age.maxAge)
                commandBuffer.DestroyEntity(entityInQueryIndex, entity);

        }).ScheduleParallel();
        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}