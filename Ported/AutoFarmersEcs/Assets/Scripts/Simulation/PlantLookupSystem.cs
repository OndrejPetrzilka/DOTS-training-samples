using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
public class PlantLookupSystem : SystemBase
{
    struct PlantLookupData : ISystemStateComponentData
    {
        public int2 Position;
    }

    EntityQuery m_deletedPlants;
    EntityCommandBufferSystem m_cmdBuffer;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_deletedPlants = GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(PlantLookupData) }, None = new ComponentType[] { typeof(PlantTag) }, });

        EntityManager.AddBuffer<PlantLookup>(EntityManager.CreateEntity());
        m_cmdBuffer = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().mapSize;
        var lookup = EntityManager.AddBuffer<PlantLookup>(GetSingletonEntity<PlantLookup>());
        if (lookup.Length == 0)
        {
            lookup.Length = mapSize.x * mapSize.y;
            for (int i = 0; i < lookup.Length; i++)
            {
                lookup.ElementAt(i) = default;
            }
        }

        var buffer = m_cmdBuffer.CreateCommandBuffer();

        // Plants are immovable, change filter to update position not needed

        // Add new Plants
        Entities.WithStructuralChanges().WithAll<PlantTag>().WithNone<PlantLookupData>().ForEach((Entity e, in Position position) =>
        {
            PlantLookupData data;
            data.Position = (int2)position.Value;
            int index = data.Position.x + data.Position.y * mapSize.x;
            lookup.ElementAt(index).Entity = e;
            buffer.AddComponent(e, data);
        }).Run();

        // Remove from lookup
        Entities.WithNone<PlantTag>().ForEach((Entity e, in PlantLookupData data) =>
        {
            int index = data.Position.x + data.Position.y * mapSize.x;
            lookup.ElementAt(index).Entity = Entity.Null;
        }).Run();

        // Remove components
        EntityManager.RemoveComponent(m_deletedPlants, typeof(PlantLookupData));

        m_cmdBuffer.AddJobHandleForProducer(Dependency);
    }
}
