using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Adds entities with specific component into lookup buffer, where they can be found by position index.
/// </summary>
[UpdateInGroup(typeof(LookupGroup))]
[AlwaysUpdateSystem]
public class LookupSystem : SystemBase
{
    protected struct Element
    {
        public EntityQuery AddedQuery;
        public ComponentType ComponentType;
        public int ComponentTypeIndex;
    }

    protected struct LookupInternalData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
        public int ComponentTypeIndex;
    }

    struct RemoveJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle Entities;

        [ReadOnly]
        public Entity Singleton;

        public int MapWidth;

        [ReadOnly]
        public ComponentTypeHandle<LookupInternalData> DataHandle;

        public BufferFromEntity<LookupEntity> EntityLookup;
        public BufferFromEntity<LookupData> EntityLookupData;

        public EntityCommandBuffer Buffer;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(Entities);

            var lookupBuffer = EntityLookup[Singleton];
            var lookupDataBuffer = EntityLookupData[Singleton];
            var datas = chunk.GetNativeArray(DataHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var data = datas[i];
                SetLookupData(lookupBuffer, lookupDataBuffer, Entity.Null, default, data.Position, data.Size, MapWidth);
                Buffer.RemoveComponent(entities[i], typeof(LookupInternalData));
            }
        }
    }

    struct AddJob : IJobChunk
    {
        [ReadOnly]
        public EntityTypeHandle Entities;

        [ReadOnly]
        public Entity Singleton;

        public int ComponentTypeIndex;
        public int MapWidth;

        [ReadOnly]
        public ComponentTypeHandle<Position> Positions;

        [ReadOnly]
        public ComponentTypeHandle<Size> Sizes;

        [ReadOnly]
        public ComponentTypeHandle<LookupComponentFilters> Filters;

        public EntityCommandBuffer Buffer;

        public BufferFromEntity<LookupEntity> EntityLookup;
        public BufferFromEntity<LookupData> EntityLookupData;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(Entities);
            var positions = chunk.GetNativeArray(Positions);
            var sizes = chunk.Has(Sizes) ? chunk.GetNativeArray(Sizes) : default;
            var filters = chunk.Has(Filters) ? chunk.GetNativeArray(Filters) : default;

            var lookupBuffer = EntityLookup[Singleton];
            var lookupDataBuffer = EntityLookupData[Singleton];

            for (int i = 0; i < chunk.Count; i++)
            {
                var e = entities[i];
                var position = positions[i];

                int2 size = sizes.IsCreated ? (int2)sizes[i].Value : int2.zero;
                byte filter = filters.IsCreated ? filters[i].Value : default;

                LookupData element = new LookupData(ComponentTypeIndex, filter);
                LookupInternalData data = new LookupInternalData { Position = (int2)position.Value, Size = size, ComponentTypeIndex = ComponentTypeIndex };
                SetLookupData(lookupBuffer, lookupDataBuffer, e, element, data.Position, data.Size, MapWidth);
                Buffer.AddComponent(e, data);
            }
        }
    }

    EntityQuery m_changedEntitiesQuery;
    EntityQuery m_deletedQuery;
    List<Element> m_elements = new List<Element>();
    EntityCommandBufferSystem m_cmdSystem;
    Entity m_lookup;
    bool m_initialized = false;

    public EntityQuery DeletedQuery
    {
        get { return m_deletedQuery; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_lookup = EntityManager.CreateEntity();
        EntityManager.SetName(m_lookup, "Lookup");
        EntityManager.AddBuffer<LookupEntity>(m_lookup);
        EntityManager.AddBuffer<LookupData>(m_lookup);

        Register(typeof(RockTag));
        Register(typeof(StoreTag));
        Register(typeof(PlantTag));
    }

    public void Register(Type componentType)
    {
        Element element;
        element.ComponentType = componentType;
        element.ComponentTypeIndex = TypeManager.GetTypeIndex(componentType);
        element.AddedQuery = Query.WithAll(componentType).WithNone<LookupInternalData>();
        m_elements.Add(element);

        var desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(LookupInternalData) },
            None = m_elements.Select(s => s.ComponentType).ToArray(),
        };

        m_deletedQuery = EntityManager.CreateEntityQuery(desc);
    }

    protected override void OnDestroy()
    {
        m_initialized = false;
        m_lookup = Entity.Null;
        EntityManager.DestroyEntity(m_lookup);
        base.OnDestroy();
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        if (!m_initialized)
        {
            EntityManager.GetBuffer<LookupEntity>(m_lookup).Initialize(Settings.MapSize.x * Settings.MapSize.y);
            EntityManager.GetBuffer<LookupData>(m_lookup).Initialize(Settings.MapSize.x * Settings.MapSize.y);
            m_initialized = true;
        }
    }

    protected override void OnUpdate()
    {
        RemoveDeletedEntities();
        UpdateEntitiesWithChangedFilters();
        AddNewEntities();
    }

    private void RemoveDeletedEntities()
    {
        if (m_deletedQuery != default && !m_deletedQuery.IsEmptyIgnoreFilter)
        {
            // Remove deleted
            RemoveJob job;
            job.DataHandle = GetComponentTypeHandle<LookupInternalData>(true);
            job.EntityLookup = GetBufferFromEntity<LookupEntity>(false);
            job.EntityLookupData = GetBufferFromEntity<LookupData>(false);
            job.Singleton = m_lookup;
            job.MapWidth = Settings.MapSize.x;
            job.Entities = GetEntityTypeHandle();
            job.Buffer = m_cmdSystem.CreateCommandBuffer();
            Dependency = job.ScheduleSingle(m_deletedQuery, Dependency); // TODO: Could schedule parallel, entities don't overlap, even if they do, writing null is safe

            // Remove system component - this does not work, remove component must be done in job to work, don't know why
            //m_cmdSystem.CreateCommandBuffer().RemoveComponent(m_deletedQuery, typeof(LookupInternalData));
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }

    private void AddNewEntities()
    {
        //NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(m_elements.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        for (int i = 0; i < m_elements.Count; i++)
        {
            // TODO: Could run in parallel, entities don't overlap
            var element = m_elements[i];
            if (!element.AddedQuery.IsEmpty)
            {
                AddJob job;
                job.Entities = GetEntityTypeHandle();
                job.Positions = GetComponentTypeHandle<Position>(true);
                job.Sizes = GetComponentTypeHandle<Size>(true);
                job.Filters = GetComponentTypeHandle<LookupComponentFilters>(true);
                job.EntityLookup = GetBufferFromEntity<LookupEntity>(false);
                job.EntityLookupData = GetBufferFromEntity<LookupData>(false);
                job.Singleton = m_lookup;
                job.ComponentTypeIndex = element.ComponentTypeIndex;
                job.MapWidth = Settings.MapSize.x;
                job.Buffer = m_cmdSystem.CreateCommandBuffer();
                Dependency = job.ScheduleSingle(element.AddedQuery, Dependency); // TODO: Could schedule parallel, entities don't overlap
                m_cmdSystem.AddJobHandleForProducer(Dependency);
            }
        }

        //var result = JobHandle.CombineDependencies(dependencies);
        //dependencies.Dispose();
        //m_cmdSystem.AddJobHandleForProducer(result);
    }

    private void UpdateEntitiesWithChangedFilters()
    {
        if (!m_changedEntitiesQuery.IsEmpty)
        {
            var mapSize = Settings.MapSize;
            var singleton = m_lookup;
            var entityLookup = GetBufferFromEntity<LookupEntity>(false);
            var entityLookupData = GetBufferFromEntity<LookupData>(false);
            var sizes = GetComponentDataFromEntity<Size>(true);

            // Handle changed LookupComponentFilters
            Entities.WithReadOnly(sizes).WithChangeFilter<LookupComponentFilters>().WithStoreEntityQueryInField(ref m_changedEntitiesQuery).ForEach((Entity e, in LookupInternalData data, in Position position, in LookupComponentFilters filter) =>
            {
                int2 size = sizes.HasComponent(e) ? (int2)sizes[e].Value : int2.zero;

                LookupData element = new LookupData(data.ComponentTypeIndex, filter.Value);
                SetLookupFilter(entityLookupData[singleton], element, (int2)position.Value, size, mapSize.x);
            }).Schedule(); // TODO: Could schedule parallel, entities don't overlap, even if they do, writing null is safe
        }
    }

    protected static void SetLookupData(DynamicBuffer<LookupEntity> entityArray, DynamicBuffer<LookupData> dataArray, Entity e, LookupData data, int2 pos, int2 size, int mapWidth)
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

    protected static void SetLookupFilter(DynamicBuffer<LookupData> dataArray, LookupData data, int2 pos, int2 size, int mapWidth)
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
