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

[UpdateInGroup(typeof(LookupGroup))]
public class LookupSystem : SystemBase
{
    struct LookupInternalData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
    }

    EntityQuery m_deletedQuery;
    EntityCommandBufferSystem m_cmdSystem;
    Settings m_settings;
    Entity m_lookup;
    DynamicBuffer<LookupEntity> m_lookupEntityBuffer;
    DynamicBuffer<LookupData> m_lookupDataBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        m_settings = EntityManager.CreateEntityQuery(typeof(Settings)).GetSingleton<Settings>();

        m_lookup = EntityManager.CreateEntity();
        EntityManager.SetName(m_lookup, "Lookup");
        m_lookupEntityBuffer = EntityManager.AddBuffer<LookupEntity>(m_lookup);
        m_lookupDataBuffer = EntityManager.AddBuffer<LookupData>(m_lookup);
        m_lookupEntityBuffer.Initialize(m_settings.mapSize.x * m_settings.mapSize.y);
        m_lookupDataBuffer.Initialize(m_settings.mapSize.x * m_settings.mapSize.y);
    }

    protected override void OnStopRunning()
    {
        m_lookupEntityBuffer = default;
        m_lookupDataBuffer = default;
        EntityManager.DestroyEntity(m_lookup);
    }

    protected override void OnUpdate()
    {
        var mapSize = m_settings.mapSize;
        Entity singleton = m_lookup;

        // TODO: Change monitoring for: Position, Size, LookupComponent
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
