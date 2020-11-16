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
public class FarmerClearRocks : SystemBase
{
    static readonly ComponentTypes m_jobFinishedTypes = new ComponentTypes(typeof(WorkClearRocks), typeof(PathFinished), typeof(PathTarget));

    EntityQuery m_rocks;
    EntityQuery m_pathFailed;
    EntityQuery m_needsPath;
    EntityQuery m_targetReached;

    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_rocks = EntityManager.CreateEntityQuery(typeof(RockTag));
        m_pathFailed = Query.WithAll<FarmerTag, WorkClearRocks, PathFailed>();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;

        if (m_rocks.IsEmptyIgnoreFilter)
        {
            // All rocks cleared, remove work
            m_cmdSystem.CreateCommandBuffer().RemoveComponent<WorkClearRocks>(m_needsPath);
            m_cmdSystem.AddJobHandleForProducer(Dependency);
            return;
        }

        // Remove work when path finding failed
        if (!m_pathFailed.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent<WorkClearRocks>(m_pathFailed);
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Add FindPath component
        if (!m_needsPath.IsEmptyIgnoreFilter)
        {
            var findPath = FindPath.Create<RockTag>();
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag, WorkClearRocks>().WithNone<FindPath, PathTarget, PathFailed>().WithStoreEntityQueryInField(ref m_needsPath).ForEach((Entity e, int entityInQueryIndex) =>
            {
                cmdBuffer.AddComponent(entityInQueryIndex, e, findPath);
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Reached target
        if (!m_targetReached.IsEmptyIgnoreFilter)
        {
            var jobFinishedTypes = m_jobFinishedTypes;
            var positions = GetComponentDataFromEntity<Position>(true);
            var healths = GetComponentDataFromEntity<Health>(false);
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag, WorkClearRocks, PathFinished>().WithReadOnly(positions).WithStoreEntityQueryInField(ref m_targetReached).ForEach((Entity e, int entityInQueryIndex, ref Offset offset, ref RandomState rng, in SmoothPosition smoothPosition, in PathTarget target) =>
            {
                // Attack rock
                Health health = default;
                if (positions.HasComponent(target.Entity))
                {
                    var rockPosition = positions[target.Entity].Value;
                    health = healths[target.Entity];
                    health.Value -= 1;
                    healths[target.Entity] = health;

                    if (health.Value <= 0)
                    {
                        cmdBuffer.DestroyEntity(entityInQueryIndex, target.Entity);
                    }

                    offset.Value = math.normalizesafe(rockPosition - smoothPosition.Value) * 0.5f * rng.Rng.NextFloat();
                }
                if (health.Value <= 0)
                {
                    cmdBuffer.RemoveComponent(entityInQueryIndex, e, jobFinishedTypes);
                }
            }).Schedule();
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
