using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;

/// <summary>
/// Adds entities with <see cref="LookupComponent"/> into lookup buffer, where they can be found by position index.
/// </summary>
[UpdateInGroup(typeof(LookupGroup))]
[DisableAutoCreation]
public class LookupSystem_obsolete : SystemBase
{
    struct LookupInternalData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
    }

    EntityQuery m_deletedQuery;
    EntityCommandBufferSystem m_cmdSystem;
    WorldSettings m_settings;
    Entity m_lookup;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_lookup = EntityManager.CreateEntity();
        EntityManager.SetName(m_lookup, "Lookup");
        EntityManager.AddBuffer<LookupEntity>(m_lookup);
        EntityManager.AddBuffer<LookupData>(m_lookup);
    }

    protected override void OnDestroy()
    {
        m_lookup = Entity.Null;
        EntityManager.DestroyEntity(m_lookup);
        base.OnDestroy();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        if (m_settings.MapSize.Equals(int2.zero))
        {
            m_settings = EntityManager.CreateEntityQuery(typeof(WorldSettings)).GetSingleton<WorldSettings>();
            EntityManager.GetBuffer<LookupEntity>(m_lookup).Initialize(m_settings.MapSize.x * m_settings.MapSize.y);
            EntityManager.GetBuffer<LookupData>(m_lookup).Initialize(m_settings.MapSize.x * m_settings.MapSize.y);
        }
    }

    protected override void OnUpdate()
    {
        var mapSize = m_settings.MapSize;
        Entity singleton = m_lookup;

        var entityLookup = GetBufferFromEntity<LookupEntity>(false);
        var entityLookupData = GetBufferFromEntity<LookupData>(false);

        // Remove deleted
        Entities.WithNone<LookupComponent>().WithStoreEntityQueryInField(ref m_deletedQuery).ForEach((Entity e, in LookupInternalData data) =>
        {
            SetLookupData(entityLookup[singleton], entityLookupData[singleton], Entity.Null, default, data.Position, data.Size, mapSize.x);
        }).Schedule();

        var cmdBuffer = m_cmdSystem.CreateCommandBuffer();

        // Remove components
        cmdBuffer.RemoveComponent(m_deletedQuery, typeof(LookupInternalData));

        entityLookupData = GetBufferFromEntity<LookupData>(false);
        ComponentDataFromEntity<Size> sizes = GetComponentDataFromEntity<Size>(true);

        // Handle changed LookupComponentFilters
        Entities.WithReadOnly(sizes).WithChangeFilter<LookupComponentFilters>().ForEach((Entity e, in LookupComponent lookup, in Position position, in LookupComponentFilters filter) =>
        {
            int2 size = sizes.HasComponent(e) ? (int2)sizes[e].Value : int2.zero;

            LookupData element = new LookupData(lookup.ComponentTypeIndex, filter.Value);
            SetLookupFilter(entityLookupData[singleton], element, (int2)position.Value, size, mapSize.x);
        }).Schedule();

        entityLookup = GetBufferFromEntity<LookupEntity>(false);
        ComponentDataFromEntity<LookupComponentFilters> filters = GetComponentDataFromEntity<LookupComponentFilters>(true);
        entityLookupData = GetBufferFromEntity<LookupData>(false);
        sizes = GetComponentDataFromEntity<Size>(true);

        // Add new
        Entities.WithReadOnly(filters).WithReadOnly(sizes).WithNone<LookupInternalData>().ForEach((Entity e, in LookupComponent lookup, in Position position) =>
        {
            int2 size = sizes.HasComponent(e) ? (int2)sizes[e].Value : int2.zero;
            byte filter = filters.HasComponent(e) ? filters[e].Value : default;

            LookupData element = new LookupData(lookup.ComponentTypeIndex, filter);
            LookupInternalData data = new LookupInternalData { Position = (int2)position.Value, Size = size };
            SetLookupData(entityLookup[singleton], entityLookupData[singleton], e, element, data.Position, data.Size, mapSize.x);
            cmdBuffer.AddComponent(e, data);
        }).Schedule();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }

    static void SetLookupData(DynamicBuffer<LookupEntity> entityArray, DynamicBuffer<LookupData> dataArray, Entity e, LookupData data, int2 pos, int2 size, int mapWidth)
    {
        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                int2 p = pos + new int2(x, y);
                int index = p.x + p.y * mapWidth;
                entityArray[index] = new LookupEntity { Entity = e };
                dataArray[index] = data;
            }
        }
    }

    static void SetLookupFilter(DynamicBuffer<LookupData> dataArray, LookupData data, int2 pos, int2 size, int mapWidth)
    {
        for (int x = 0; x <= size.x; x++)
        {
            for (int y = 0; y <= size.y; y++)
            {
                int2 p = pos + new int2(x, y);
                int index = p.x + p.y * mapWidth;
                dataArray[index] = data;
            }
        }
    }
}
