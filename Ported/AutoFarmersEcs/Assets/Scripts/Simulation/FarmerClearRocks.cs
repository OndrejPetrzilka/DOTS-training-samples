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
public class FarmerClearRocks : SystemBase
{
    EntityQuery m_rocks;
    EntityQuery m_needsPath;
    EntityQuery m_noTarget;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_rocks = Query.WithAll<RockTag>();
        m_needsPath = Query.WithAll<FarmerTag, WorkClearRocks>().WithNone<FindPath, PathData>();
        m_noTarget = Query.WithAll<FarmerTag, WorkClearRocks, PathData>().WithNone<PathTarget>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().mapSize;

        if (m_rocks.CalculateChunkCountWithoutFiltering() == 0)
        {
            // All rocks cleared, remove work
            EntityManager.RemoveComponent<WorkClearRocks>(m_needsPath);
            return;
        }

        // Add FindPath component
        this.AddComponentData(m_needsPath, FindPath.Create<RockTag>());

        // Remove work when there's no target
        EntityManager.RemoveComponent<WorkClearRocks>(m_noTarget);

        // Reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkClearRocks, PathFinished>().ForEach((Entity e, DynamicBuffer<PathData> path, in PathTarget target) =>
        {
            // Attack rock
            int health = 0;
            if (EntityManager.Exists(target.Entity))
            {
                var rockPosition = EntityManager.GetComponentData<Position>(target.Entity).Value;
                health = EntityManager.GetComponentData<Health>(target.Entity).Value - 1;
                EntityManager.SetComponentData(target.Entity, new Health { Value = health });
                if (health <= 0)
                {
                    EntityManager.DestroyEntity(target.Entity);
                }

                var pos = EntityManager.GetComponentData<SmoothPosition>(e).Value;
                Offset offset = default;
                offset.Value = math.normalizesafe(rockPosition - pos) * 0.5f * Random.value;
                EntityManager.SetComponentData(e, offset);
            }
            if (health <= 0)
            {
                EntityManager.RemoveComponent<WorkClearRocks>(e);
                EntityManager.RemoveComponent<PathFinished>(e);
                EntityManager.RemoveComponent<PathData>(e);
                EntityManager.RemoveComponent<PathTarget>(e);
            }
        }).Run();
    }
}