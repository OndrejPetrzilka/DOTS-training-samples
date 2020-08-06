//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;

//[assembly: RegisterGenericComponentType(typeof(Lookup<RockTag>))]
//[assembly: RegisterGenericComponentType(typeof(Lookup<PlantTag>))]
//[assembly: RegisterGenericComponentType(typeof(Lookup<StoreTag>))]

//[assembly: RegisterGenericComponentType(typeof(LookupSystem<RockTag>.LookupInternalData))]
//[assembly: RegisterGenericComponentType(typeof(LookupSystem<PlantTag>.LookupInternalData))]
//[assembly: RegisterGenericComponentType(typeof(LookupSystem<StoreTag>.LookupInternalData))]

//[assembly: RegisterGenericComponentType(typeof(LookupSystem<RockTag>.LookupInternalDataSize))]
//[assembly: RegisterGenericComponentType(typeof(LookupSystem<PlantTag>.LookupInternalDataSize))]
//[assembly: RegisterGenericComponentType(typeof(LookupSystem<StoreTag>.LookupInternalDataSize))]

//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
//public class LookupSystem<TTag> : SystemBase
//    where TTag : struct, IComponentData
//{
//    public struct LookupInternalData : ISystemStateComponentData
//    {
//        public int2 Position;
//    }

//    public struct LookupInternalDataSize : ISystemStateComponentData
//    {
//        public int2 Position;
//        public int2 Size;
//    }

//    protected struct AddNewJob : IJobChunk
//    {
//        public Entity LookupEntity;
//        public BufferFromEntity<Lookup<TTag>> LookupArray;
//        public int MapWidth;

//        public EntityCommandBuffer CmdBuffer;
//        [ReadOnly]
//        public EntityTypeHandle Entity;

//        [ReadOnly]
//        public ComponentTypeHandle<Position> Position;
//        [ReadOnly]
//        public ComponentTypeHandle<Size> Size;

//        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//        {
//            var lookup = LookupArray[LookupEntity];
//            var entities = chunk.GetNativeArray(Entity);
//            var positions = chunk.GetNativeArray(Position);
//            if (chunk.Has(Size))
//            {
//                var sizes = chunk.GetNativeArray(Size);
//                for (int i = 0; i < chunk.Count; i++)
//                {
//                    LookupInternalDataSize data = new LookupInternalDataSize { Position = (int2)positions[i].Value, Size = (int2)sizes[i].Value };
//                    SetLookupData(lookup, entities[i], data.Position, data.Size, MapWidth);
//                    CmdBuffer.AddComponent(entities[i], data);
//                }
//            }
//            else
//            {
//                for (int i = 0; i < chunk.Count; i++)
//                {
//                    LookupInternalData data = new LookupInternalData { Position = (int2)positions[i].Value };
//                    SetLookupData(lookup, entities[i], data.Position, MapWidth);
//                    CmdBuffer.AddComponent(entities[i], data);
//                }
//            }
//        }
//    }

//    protected struct RemoveDeletedJob : IJobChunk
//    {
//        public Entity LookupEntity;
//        public BufferFromEntity<Lookup<TTag>> LookupArray;
//        public int MapWidth;

//        [ReadOnly]
//        public ComponentTypeHandle<LookupInternalData> Data;
//        [ReadOnly]
//        public ComponentTypeHandle<LookupInternalDataSize> DataWithSize;

//        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//        {
//            var lookup = LookupArray[LookupEntity];
//            if (chunk.Has(DataWithSize))
//            {
//                var data = chunk.GetNativeArray(DataWithSize);
//                for (int i = 0; i < chunk.Count; i++)
//                {
//                    SetLookupData(lookup, Entity.Null, data[i].Position, data[i].Size, MapWidth);
//                }
//            }
//            else
//            {
//                var data = chunk.GetNativeArray(Data);
//                for (int i = 0; i < chunk.Count; i++)
//                {
//                    SetLookupData(lookup, Entity.Null, data[i].Position, MapWidth);
//                }
//            }
//        }
//    }

//    protected EntityQuery m_deleted;
//    protected EntityQuery m_addNew;
//    protected EntityQuery m_removeDeleted;
//    protected EntityQuery m_lookupSingleton;
//    protected EntityCommandBufferSystem m_cmdSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        m_deleted = GetEntityQuery(new EntityQueryDesc() { Any = new ComponentType[] { typeof(LookupInternalData), typeof(LookupInternalDataSize) }, None = new ComponentType[] { typeof(TTag) }, });
//        m_addNew = GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(TTag), typeof(Position) }, None = new ComponentType[] { typeof(LookupInternalData), typeof(LookupInternalDataSize) }, });
//        m_removeDeleted = GetEntityQuery(new EntityQueryDesc() { Any = new ComponentType[] { typeof(LookupInternalData), typeof(LookupInternalDataSize) }, None = new ComponentType[] { typeof(TTag) } });
//        m_lookupSingleton = GetEntityQuery(typeof(Lookup<TTag>));

//        EntityManager.AddBuffer<Lookup<TTag>>(EntityManager.CreateEntity());
//        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

//        // Why???
//        var group = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();
//        group.AddSystemToUpdateList(this);
//    }

//    protected override void OnStartRunning()
//    {
//        base.OnStartRunning();
//        var mapSize = this.GetSettings().mapSize;
//        var lookup = this.GetSingleton<Lookup<TTag>>();
//        if (lookup.Length == 0)
//        {
//            lookup.Length = mapSize.x * mapSize.y;
//            for (int i = 0; i < lookup.Length; i++)
//            {
//                lookup[i] = default;
//            }
//        }

//        // TODO: To make correct load add all entities currently in world
//    }

//    protected override void OnUpdate()
//    {
//        var mapSize = this.GetSettings().mapSize;
//        var cmdBuffer = m_cmdSystem.CreateCommandBuffer();

//        // Considering objects are immovable

//        Entity entity = m_lookupSingleton.GetSingletonEntity();
//        BufferFromEntity<Lookup<TTag>> lookups = GetBufferFromEntity<Lookup<TTag>>();

//        Dependency = new AddNewJob()
//        {
//            LookupEntity = entity,
//            LookupArray = lookups,
//            MapWidth = mapSize.x,
//            CmdBuffer = cmdBuffer,
//            Entity = GetEntityTypeHandle(),
//            Position = GetComponentTypeHandle<Position>(true),
//            Size = GetComponentTypeHandle<Size>(true)
//        }.ScheduleSingle(m_addNew, Dependency);

//        Dependency = new RemoveDeletedJob()
//        {
//            LookupEntity = entity,
//            LookupArray = lookups,
//            MapWidth = mapSize.x,
//            Data = GetComponentTypeHandle<LookupInternalData>(true),
//            DataWithSize = GetComponentTypeHandle<LookupInternalDataSize>(true)
//        }.ScheduleSingle(m_removeDeleted, Dependency);


//        // Add new
//        //Entities.WithAll<TTag>().WithNone<Size, LookupInternalData>().ForEach((Entity e, in Position position) =>
//        //{
//        //    LookupInternalData data = new LookupInternalData { Position = (int2)position.Value };
//        //    SetLookupData(lookups[entity], e, data.Position, mapSize.x);
//        //    cmdBuffer.AddComponent(e, data);
//        //}).Schedule();

//        //m_cmdSystem.AddJobHandleForProducer(Dependency);

//        //// Remove deleted
//        //Entities.WithNone<TTag>().ForEach((Entity e, in LookupInternalData data) =>
//        //{
//        //    SetLookupData(lookups[entity], Entity.Null, data.Position, mapSize.x);
//        //}).Schedule();


//        //// Add new size
//        //Entities.WithAll<TTag>().WithNone<LookupInternalDataSize>().ForEach((Entity e, in Position position, in Size size) =>
//        //{
//        //    LookupInternalDataSize data = new LookupInternalDataSize { Position = (int2)position.Value, Size = (int2)size.Value };
//        //    SetLookupData(lookups[entity], e, data.Position, data.Size, mapSize.x);
//        //    cmdBuffer.AddComponent(e, data);
//        //}).Schedule();

//        //// Remove deleted with size
//        //Entities.WithNone<TTag>().ForEach((Entity e, in LookupInternalDataSize data) =>
//        //{
//        //    SetLookupData(lookups[entity], Entity.Null, data.Position, data.Size, mapSize.x);
//        //}).Schedule();

//        m_cmdSystem.AddJobHandleForProducer(Dependency);

//        // Remove components
//        EntityManager.RemoveComponent(m_deleted, new ComponentTypes(typeof(LookupInternalData), typeof(LookupInternalDataSize)));
//    }

//    static void SetLookupData(DynamicBuffer<Lookup<TTag>> lookup, Entity e, int2 pos, int mapWidth)
//    {
//        lookup[pos.x + pos.y * mapWidth] = e;
//    }

//    static void SetLookupData(DynamicBuffer<Lookup<TTag>> lookup, Entity e, int2 pos, int2 size, int mapWidth)
//    {
//        for (int x = 0; x <= size.x; x++)
//        {
//            for (int y = 0; y <= size.y; y++)
//            {
//                int2 p = pos + new int2(x, y);
//                int index = p.x + p.y * mapWidth;
//                lookup.ElementAt(index).Entity = e;
//            }
//        }
//    }
//}
