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
    EntityQuery m_rocks;
    EntityQuery m_needsPath;
    EntityQuery m_noTarget;

    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_rocks = Query.WithAll<RockTag>();
        m_needsPath = Query.WithAll<FarmerTag, WorkClearRocks>().WithNone<FindPath, PathData>();
        m_noTarget = Query.WithAll<FarmerTag, WorkClearRocks, PathData>().WithNone<PathTarget>();
    }

    protected override void OnUpdate()
    {
        var mapSize = this.GetSettings().MapSize;

        if (m_rocks.IsEmptyIgnoreFilter)
        {
            // All rocks cleared, remove work
            m_cmdSystem.CreateCommandBuffer().RemoveComponent<WorkClearRocks>(m_needsPath);
            m_cmdSystem.AddJobHandleForProducer(Dependency);
            return;
        }

        var singleCmdBuffer = m_cmdSystem.CreateCommandBuffer();

        // Remove work when there's no target
        singleCmdBuffer.RemoveComponent<WorkClearRocks>(m_noTarget);

        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        // Add FindPath component
        var findPath = FindPath.Create<RockTag>();
        Entities.WithAll<FarmerTag, WorkClearRocks>().WithNone<FindPath, PathData>().ForEach((Entity e, int entityInQueryIndex) =>
        {
            cmdBuffer.AddComponent(entityInQueryIndex, e, findPath);
        }).Schedule();

        var positions = GetComponentDataFromEntity<Position>(true);
        var healths = GetComponentDataFromEntity<Health>(false);

        var jobFinishedTypes = new ComponentTypes(typeof(WorkClearRocks), typeof(PathFinished), typeof(PathData), typeof(PathTarget));

        // Reached target
        Entities.WithAll<FarmerTag, WorkClearRocks, PathFinished>().WithReadOnly(positions).ForEach((Entity e, int entityInQueryIndex, DynamicBuffer<PathData> path, ref Offset offset, ref RandomState rng, in SmoothPosition smoothPosition, in PathTarget target) =>
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