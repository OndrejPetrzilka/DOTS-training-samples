using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FarmGroup))]
public class FarmerPlantSeeds : SystemBase
{
    struct BuyingSeedsTag : IComponentData
    {
    }

    EntityCommandBufferSystem m_cmdSystem;
    EntityArchetype m_plantArchetype;
    EntityQuery m_needsSeeds;
    EntityQuery m_buySeeds;
    EntityQuery m_buyingSeeds;
    EntityQuery m_needsPlantTarget;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<LookupData>();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_plantArchetype = EntityManager.CreateArchetype(typeof(PlantTag), typeof(Position));
        m_needsSeeds = Query.WithAll<FarmerTag, WorkPlantSeeds>().WithNone<HasSeedsTag, PathTarget>();
        m_buySeeds = Query.WithAll<FarmerTag, WorkPlantSeeds, PathFinished>().WithNone<HasSeedsTag>();
        m_buyingSeeds = Query.WithAll<FarmerTag, BuyingSeedsTag>();
        m_needsPlantTarget = Query.WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithNone<PathTarget>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;

        // Go to store to buy seeds
        this.AddComponentData(m_needsSeeds, FindPath.Create<StoreTag>());

        // Does not have seeds, reached target, buy seeds
        EntityManager.AddComponent(m_buySeeds, new ComponentTypes(typeof(BuyingSeedsTag), typeof(HasSeedsTag)));
        EntityManager.RemoveComponent(m_buyingSeeds, new ComponentTypes(new ComponentType[] { typeof(BuyingSeedsTag), typeof(PathFinished), typeof(PathData), typeof(PathTarget) }));

        // Find plant target
        this.AddComponentData(m_needsPlantTarget, new FindPath(-1, 0, FindPathFlags.UseGroundState | FindPathFlags.GroundStateTilled));

        var lookup = this.GetSingleton<LookupData>();

        // Reached target
        var plantArchetype = m_plantArchetype;
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
        var planedComponentTypes = new ComponentTypes(typeof(PathFinished), typeof(PathData), typeof(PathTarget));
        Entities.WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithAll<PathFinished>().ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in Position position) =>
        {
            // Plant seeds
            int2 tile = (int2)math.floor(position.Value);

            // Check there's no plant
            if (lookup[tile.x + tile.y * mapSize.x].ComponentTypeIndex == -1)
            {
                // Spawn plant
                int seed = Mathf.FloorToInt(Mathf.PerlinNoise(tile.x / 10f, tile.y / 10f) * 10) + 317281687;

                var plant = cmdBuffer.CreateEntity(entityInQueryIndex, plantArchetype);
                cmdBuffer.SetComponent(entityInQueryIndex, plant, new PlantTag { Seed = 0, Growth = 0 });
                cmdBuffer.SetComponent(entityInQueryIndex, plant, new Position { Value = tile });
            }

            // Remove target
            cmdBuffer.RemoveComponent(entityInQueryIndex, e, planedComponentTypes);

            // Choose other work
            if (rng.Rng.NextFloat() < 0.1f)
            {
                cmdBuffer.RemoveComponent<WorkPlantSeeds>(entityInQueryIndex, e);
            }
        }).Schedule();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}