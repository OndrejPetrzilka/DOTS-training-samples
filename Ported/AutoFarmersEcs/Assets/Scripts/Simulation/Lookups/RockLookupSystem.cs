using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
[UpdateInGroup(typeof(LookupGroup))]
public class RockLookupSystem : SystemBase
{
    struct RockLookupData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
    }

    struct RockLookup : IBufferElementData
    {
        public Entity Entity;
    }

    EntityQuery m_deletedRocks;
    EntityCommandBufferSystem m_cmdBuffer;
    WorldSettings m_settings;
    Entity m_lookup;
    DynamicBuffer<RockLookup> m_lookupBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdBuffer = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        m_settings = EntityManager.CreateEntityQuery(typeof(WorldSettings)).GetSingleton<WorldSettings>();
        m_lookup = EntityManager.CreateEntity();
        EntityManager.SetName(m_lookup, "RockLookup");
        m_lookupBuffer = EntityManager.AddBuffer<RockLookup>(m_lookup);
        m_lookupBuffer.Initialize(m_settings.MapSize.x * m_settings.MapSize.y);
    }

    protected override void OnStopRunning()
    {
        m_lookupBuffer = default;
        EntityManager.DestroyEntity(m_lookup);
    }

    protected override void OnUpdate()
    {
        var mapSize = m_settings.MapSize;
        var lookup = m_lookupBuffer;

        // Rocks are immovable, change filter to update position not needed
        var buffer = m_cmdBuffer.CreateCommandBuffer();

        // Add new rocks
        Entities.WithName("AddRocksToLookup").WithAll<RockTag>().WithNone<RockLookupData>().ForEach((Entity e, in Position position, in Size size) =>
        {
            RockLookupData data;
            data.Position = (int2)position.Value;
            data.Size = (int2)size.Value;
            SetLookupData(lookup, e, data.Position, data.Size, mapSize.x);
            buffer.AddComponent(e, data);
        }).Schedule();

        // Remove from lookup
        Entities.WithName("RemoveRocksFromLookup").WithNone<RockTag>().WithStoreEntityQueryInField(ref m_deletedRocks).ForEach((in RockLookupData data) =>
        {
            SetLookupData(lookup, Entity.Null, data.Position, data.Size, mapSize.x);
        }).Schedule();

        // Remove components
        buffer.RemoveComponent(m_deletedRocks, typeof(RockLookupData));

        m_cmdBuffer.AddJobHandleForProducer(Dependency);
    }

    static void SetLookupData(DynamicBuffer<RockLookup> lookup, Entity e, int2 pos, int2 size, int mapWidth)
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
