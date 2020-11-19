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

    static readonly ComponentTypes m_buyingSeedsComponents = new ComponentTypes(typeof(BuyingSeedsTag), typeof(HasSeedsTag));
    static readonly ComponentTypes m_boughtSeedsRemoveComponents = new ComponentTypes(new ComponentType[] { typeof(BuyingSeedsTag), typeof(PathFinished), typeof(PathData), typeof(PathTarget) });
    static readonly ComponentTypes m_planedComponentTypes = new ComponentTypes(typeof(PathFinished), typeof(PathData), typeof(PathTarget));
    static readonly ComponentTypes m_pathFailedRemoveComponents = new ComponentTypes(typeof(WorkPlantSeeds), typeof(PathData), typeof(PathFailed));

    EntityCommandBufferSystem m_cmdSystem;
    EntityArchetype m_plantArchetype;
    EntityQuery m_needsSeeds;
    EntityQuery m_buySeeds;
    EntityQuery m_buyingSeeds;
    EntityQuery m_needsPlantTarget;
    EntityQuery m_targetReached;
    EntityQuery m_pathFailed;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_plantArchetype = EntityManager.CreateArchetype(typeof(PlantTag), typeof(Position));

        m_buySeeds = Query.WithAll<FarmerTag, WorkPlantSeeds, PathFinished>().WithNone<HasSeedsTag>();
        m_buyingSeeds = Query.WithAll<FarmerTag, BuyingSeedsTag>();
        m_pathFailed = Query.WithAll<FarmerTag, WorkPlantSeeds, PathFailed>();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;

        // Go to store to buy seeds
        if (!m_needsSeeds.IsEmptyIgnoreFilter)
        {
            var findPath = FindPath.Create<StoreTag>();
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag, WorkPlantSeeds>().WithNone<FindPath, PathTarget, PathFailed>().WithNone<HasSeedsTag>().WithStoreEntityQueryInField(ref m_needsSeeds).ForEach((Entity e, int entityInQueryIndex) =>
            {
                cmdBuffer.AddComponent(entityInQueryIndex, e, findPath);
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Does not have seeds, reached target, buy seeds
        if (!m_buySeeds.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().AddComponent(m_buySeeds, m_buyingSeedsComponents);
            m_cmdSystem.CreateCommandBuffer().RemoveComponent(m_buyingSeeds, m_boughtSeedsRemoveComponents);
        }

        // Find plant target
        if (!m_needsPlantTarget.IsEmptyIgnoreFilter)
        {
            var findPlant = new FindPath(-1, 0, FindPathFlags.UseGroundState | FindPathFlags.GroundStateTilled);
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithNone<PathTarget, PathFailed>().WithStoreEntityQueryInField(ref m_needsPlantTarget).ForEach((Entity e, int entityInQueryIndex) =>
            {
                cmdBuffer.AddComponent(entityInQueryIndex, e, findPlant);
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Remove work when path finding failed
        if (!m_pathFailed.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent(m_pathFailed, m_pathFailedRemoveComponents);
        }

        // Reached target, plant seeds
        if (!m_targetReached.IsEmptyIgnoreFilter)
        {
            var lookup = GetSingletonEntity<LookupData>();
            var lookupDataArray = GetBufferFromEntity<LookupData>(true);
            var plantArchetype = m_plantArchetype;
            var planedComponentTypes = m_planedComponentTypes;
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer();
            Entities.WithReadOnly(lookupDataArray).WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithAll<PathFinished>().WithStoreEntityQueryInField(ref m_targetReached).ForEach((Entity e, ref RandomState rng, in Position position) =>
            {
                // Plant seeds
                int2 tile = (int2)math.floor(position.Value);

                // Check there's no plant
                // TODO: Check case when two farmers want to plant seeds on same position
                if (lookupDataArray[lookup][tile.x + tile.y * mapSize.x].Data == default)
                {
                    // Spawn plant
                    int seed = Mathf.FloorToInt(Mathf.PerlinNoise(tile.x / 10f, tile.y / 10f) * 10) + 317281687;

                    var plant = cmdBuffer.CreateEntity(plantArchetype);
                    cmdBuffer.SetComponent(plant, new PlantTag { Seed = seed, Growth = 0 });
                    cmdBuffer.SetComponent(plant, new Position { Value = tile });
                }

                // Remove target
                cmdBuffer.RemoveComponent(e, planedComponentTypes);

                // Choose other work
                if (rng.Rng.NextFloat() < 0.1f)
                {
                    cmdBuffer.RemoveComponent<WorkPlantSeeds>(e);
                }
            }).Schedule();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }
}