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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerPlantSeeds : SystemBase
{
    EntityArchetype m_plantArchetype;
    EntityQuery m_needsSeeds;
    EntityQuery m_needsPlantTarget;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<LookupData>();
        m_plantArchetype = EntityManager.CreateArchetype(typeof(PlantTag), typeof(Position));
        m_needsSeeds = Query.WithAll<FarmerTag, WorkPlantSeeds>().WithNone<HasSeedsTag, PathTarget>();
        m_needsPlantTarget = Query.WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithNone<PathTarget>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;

        // Go to store to buy seeds
        this.AddComponentData(m_needsSeeds, FindPath.Create<StoreTag>());

        // Does not have seeds, reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds, PathFinished>().WithNone<HasSeedsTag>().ForEach((Entity e) =>
        {
            // Buy seeds, remove target
            EntityManager.AddComponent<HasSeedsTag>(e);
            EntityManager.RemoveComponent<PathFinished>(e);
            EntityManager.RemoveComponent<PathData>(e);
            EntityManager.RemoveComponent<PathTarget>(e);

        }).Run();

        // Find plant target
        this.AddComponentData(m_needsPlantTarget, new FindPath(-1, 0, FindPathFlags.UseGroundState | FindPathFlags.GroundStateTilled));

        var lookup = this.GetSingleton<LookupData>();

        // Reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithAll<PathFinished>().ForEach((Entity e, in Position position) =>
        {
            // Plant seeds
            int2 tile = (int2)math.floor(position.Value);

            // Check there's no plant
            if (lookup[tile.x + tile.y * mapSize.x].ComponentTypeIndex == -1)
            {
                // Spawn plant
                int seed = Mathf.FloorToInt(Mathf.PerlinNoise(tile.x / 10f, tile.y / 10f) * 10) + 317281687;

                var plant = EntityManager.CreateEntity(m_plantArchetype);
                EntityManager.SetComponentData(plant, new PlantTag { Seed = 0, Growth = 0 });
                EntityManager.SetComponentData(plant, new Position { Value = tile });
            }

            // Remove target
            EntityManager.RemoveComponent<PathFinished>(e);
            EntityManager.RemoveComponent<PathData>(e);
            EntityManager.RemoveComponent<PathTarget>(e);

            // Choose other work
            if (Random.value < 0.1f)
            {
                EntityManager.RemoveComponent(e, typeof(WorkPlantSeeds));
            }
        }).Run();
    }
}