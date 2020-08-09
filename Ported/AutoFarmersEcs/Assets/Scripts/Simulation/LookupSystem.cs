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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
public class LookupSystem : SystemBase
{
    struct LookupInternalData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
    }

    EntityQuery m_deletedQuery;
    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_deletedQuery = GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(LookupInternalData) }, None = new ComponentType[] { typeof(LookupComponent) }, });

        var singleton = EntityManager.CreateEntity();
        EntityManager.SetName(singleton, "Lookup");
        EntityManager.AddBuffer<LookupEntity>(singleton);
        EntityManager.AddBuffer<LookupData>(singleton);
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().mapSize;
        Entity singleton = GetSingletonEntity<LookupEntity>();

        InitializeBuffer(EntityManager.AddBuffer<LookupEntity>(singleton), mapSize);
        InitializeBuffer(EntityManager.AddBuffer<LookupData>(singleton), mapSize);

        // TODO: Change monitoring for: Position, Size, LookupComponent

        var entityLookup = GetBufferFromEntity<LookupEntity>(false);
        var entityLookupData = GetBufferFromEntity<LookupData>(false);

        // Remove deleted
        Entities.WithNone<LookupComponent>().ForEach((Entity e, in LookupInternalData data) =>
        {
            SetLookupData(entityLookup[singleton], entityLookupData[singleton], Entity.Null, default, data.Position, data.Size, mapSize.x);
        }).Run();

        // Remove components
        EntityManager.RemoveComponent(m_deletedQuery, typeof(LookupInternalData));

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
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer();
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

    static void InitializeBuffer<T>(DynamicBuffer<T> array, int2 mapSize)
        where T : struct
    {
        if (array.Length != mapSize.x * mapSize.y)
        {
            array.Length = mapSize.x * mapSize.y;
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = default;
            }
        }
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
