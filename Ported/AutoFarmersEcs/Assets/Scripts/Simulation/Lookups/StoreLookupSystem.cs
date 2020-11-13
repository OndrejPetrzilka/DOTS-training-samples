using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(LookupGroup))]
public class StoreLookupSystem : SystemBase
{
    struct StoreLookupData : ISystemStateComponentData
    {
        public int2 Position;
    }

    EntityQuery m_deletedStores;
    EntityCommandBufferSystem m_cmdBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_deletedStores = GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(StoreLookupData) }, None = new ComponentType[] { typeof(StoreTag) }, });

        EntityManager.AddBuffer<StoreLookup>(EntityManager.CreateEntity());
        m_cmdBuffer = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().mapSize;
        var lookup = EntityManager.AddBuffer<StoreLookup>(GetSingletonEntity<StoreLookup>());
        if (lookup.Length == 0)
        {
            lookup.Length = mapSize.x * mapSize.y;
            for (int i = 0; i < lookup.Length; i++)
            {
                lookup.ElementAt(i) = default;
            }
        }

        var buffer = m_cmdBuffer.CreateCommandBuffer();

        // Stores are immovable, change filter to update position not needed

        // Add new Stores
        Entities.WithStructuralChanges().WithAll<StoreTag>().WithNone<StoreLookupData>().ForEach((Entity e, in Position position) =>
        {
            StoreLookupData data;
            data.Position = (int2)position.Value;
            int index = data.Position.x + data.Position.y * mapSize.x;
            lookup.ElementAt(index).Entity = e;
            buffer.AddComponent(e, data);
        }).Run();

        // Remove from lookup
        Entities.WithNone<StoreTag>().ForEach((Entity e, in StoreLookupData data) =>
        {
            int index = data.Position.x + data.Position.y * mapSize.x;
            lookup.ElementAt(index).Entity = Entity.Null;
        }).Run();

        // Remove components
        EntityManager.RemoveComponent(m_deletedStores, typeof(StoreLookupData));

        m_cmdBuffer.AddJobHandleForProducer(Dependency);
    }
}
