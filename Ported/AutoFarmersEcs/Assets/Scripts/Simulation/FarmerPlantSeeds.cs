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
    EntityQuery m_hasWork;
    EntityArchetype m_plantArchetype;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<Ground>();
        RequireSingletonForUpdate<RockLookup>();
        RequireSingletonForUpdate<StoreLookup>();
        RequireSingletonForUpdate<PlantLookup>();
        m_hasWork = GetEntityQuery(typeof(WorkPlantSeeds));
        m_plantArchetype = EntityManager.CreateArchetype(typeof(PlantTag), typeof(Position));
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;

        var ground = GetBuffer<Ground>(GetSingletonEntity<Ground>());
        var rocks = this.GetSingleton<RockLookup>();
        var stores = this.GetSingleton<StoreLookup>();
        var plants = this.GetSingleton<PlantLookup>();

        // Does not have seeds, does not have target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds>().WithNone<HasSeedsTag, WorkTarget>().ForEach((Entity e, in Position position) =>
        {
            // Find closest store
            float distSq = float.MaxValue;
            Entity store = Entity.Null;
            for (int i = 0; i < stores.Length; i++)
            {
                int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                float newDistSq = math.lengthsq(position.Value - pos);
                if (newDistSq < distSq)
                {
                    var newStore = stores[i].Entity;
                    if (EntityManager.Exists(newStore))
                    {
                        store = newStore;
                        distSq = newDistSq;
                    }
                }
            }

            if (store == Entity.Null)
            {
                EntityManager.RemoveComponent(e, typeof(WorkPlantSeeds));
            }
            else
            {
                EntityManager.AddComponentData(e, new WorkTarget { Value = store });
                var buffer = EntityManager.AddBuffer<PathData>(e);
                buffer.Add(new PathData { Position = EntityManager.GetComponentData<Position>(store).Value });
            }
        }).Run();

        // Does not have seeds, reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds, WorkTarget>().WithNone<HasSeedsTag, PathData>().ForEach((Entity e, in Position position) =>
        {
            // Buy seeds, remove target
            EntityManager.AddComponent<HasSeedsTag>(e);
            EntityManager.RemoveComponent<WorkTarget>(e);

        }).Run();

        // In job below it was invalid wihout this refresh, not sure why
        stores = this.GetSingleton<StoreLookup>();

        // Has seeds, does not have target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithNone<WorkTarget>().ForEach((Entity e, in Position position) =>
        {
            // Find closest tilled ground
            float distSq = float.MaxValue;
            int2 target = new int2(-1, -1);
            for (int i = 0; i < stores.Length; i++)
            {
                int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                float newDistSq = math.lengthsq(position.Value - pos);
                if (newDistSq < distSq)
                {
                    if (ground[i].State == GroundState.Tilled && plants[i].Entity == Entity.Null)
                    {
                        target = pos;
                        distSq = newDistSq;
                    }
                }
            }

            if (math.all(target >= 0))
            {
                EntityManager.AddComponentData(e, new WorkTarget { Value = Entity.Null });
                var buffer = EntityManager.AddBuffer<PathData>(e);
                buffer.Add(new PathData { Position = target });
            }
            else
            {
                EntityManager.RemoveComponent<WorkPlantSeeds>(e);
            }
        }).Run();

        // Reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkPlantSeeds, HasSeedsTag>().WithNone<PathData>().ForEach((Entity e, in WorkTarget target, in Position position) =>
        {
            // Plant seeds
            int2 tile = (int2)math.floor(position.Value);

            // Check there's no plant
            if (plants[tile.x + tile.y * mapSize.x].Entity == Entity.Null)
            {
                // Spawn plant
                int seed = Mathf.FloorToInt(Mathf.PerlinNoise(tile.x / 10f, tile.y / 10f) * 10);

                var plant = EntityManager.CreateEntity(m_plantArchetype);
                EntityManager.SetComponentData(plant, new PlantTag { Seed = 0, Growth = 0 });
                EntityManager.SetComponentData(plant, new Position { Value = tile });
            }

            // Remove target
            EntityManager.RemoveComponent(e, typeof(WorkTarget));

            // Choose other work
            if (Random.value < 0.1f)
            {
                EntityManager.RemoveComponent(e, typeof(WorkPlantSeeds));
            }
        }).Run();
    }
}