using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class RockLookupSystem : SystemBase
{
    struct RockLookupData : ISystemStateComponentData
    {
        public int2 Position;
        public int2 Size;
    }

    EntityQuery m_deletedRocks;
    EntityCommandBufferSystem m_cmdBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_deletedRocks = GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(RockLookupData) }, None = new ComponentType[] { typeof(RockTag) }, });

        EntityManager.AddBuffer<RockLookup>(EntityManager.CreateEntity());
        m_cmdBuffer = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().mapSize;
        var lookup = EntityManager.AddBuffer<RockLookup>(GetSingletonEntity<RockLookup>());
        if (lookup.Length == 0)
        {
            lookup.Length = mapSize.x * mapSize.y;
            for (int i = 0; i < lookup.Length; i++)
            {
                lookup.ElementAt(i) = default;
            }
        }

        var buffer = m_cmdBuffer.CreateCommandBuffer();

        // Rocks are immovable, change filter to update position not needed

        // Add new rocks
        Entities.WithStructuralChanges().WithAll<RockTag>().WithNone<RockLookupData>().ForEach((Entity e, in Position position, in Size size) =>
        {
            SetLookupData(lookup, e, position.Value, size.Value, mapSize.x);
            buffer.AddComponent(e, new RockLookupData { Position = position.Value, Size = size.Value });
        }).Run();

        // Remove from lookup
        Entities.WithNone<RockTag>().ForEach((Entity e, in RockLookupData data) =>
        {
            SetLookupData(lookup, Entity.Null, data.Position, data.Size, mapSize.x);
        }).Run();

        // Remove components
        EntityManager.RemoveComponent(m_deletedRocks, typeof(RockLookupData));

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
