using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
public class StoreLookupSystem : SystemBase
{
    struct StoreLookupData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
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
        Entities.WithStructuralChanges().WithAll<StoreTag>().WithNone<StoreLookupData>().ForEach((Entity e, in Position position, in Size size) =>
        {
            StoreLookupData data;
            data.Position = (int2)position.Value;
            data.Size = (int2)size.Value;
            SetLookupData(lookup, e, data.Position, data.Size, mapSize.x);
            buffer.AddComponent(e, data);
        }).Run();

        // Remove from lookup
        Entities.WithNone<StoreTag>().ForEach((Entity e, in StoreLookupData data) =>
        {
            SetLookupData(lookup, Entity.Null, data.Position, data.Size, mapSize.x);
        }).Run();

        // Remove components
        EntityManager.RemoveComponent(m_deletedStores, typeof(StoreLookupData));

        m_cmdBuffer.AddJobHandleForProducer(Dependency);
    }

    static void SetLookupData(DynamicBuffer<StoreLookup> lookup, Entity e, int2 pos, int2 size, int mapWidth)
    {
        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                int2 p = pos + new int2(x, y);
                int index = p.x + p.y * mapWidth;
                lookup.ElementAt(index).Entity = e;
            }
        }
    }
}
