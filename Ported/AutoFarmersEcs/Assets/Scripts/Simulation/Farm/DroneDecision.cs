using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(FarmGroup))]
public class DroneDecision : SystemBase
{
    public static uint JobMask;

    static readonly ComponentTypes m_failRemoveComponents = new ComponentTypes(new ComponentType[] { typeof(PathFailed), typeof(PathData), typeof(WorkClearRocks), typeof(WorkPlantSeeds), typeof(WorkSellPlants), typeof(WorkTillGround) });

    EntityCommandBufferSystem m_cmdSystem;
    EntityQuery m_addRngQuery;
    EntityQuery m_selectJobQuery;
    EntityQuery m_failedQuery;

    protected override void OnCreate()
    {
        JobMask = uint.MaxValue;
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_failedQuery = Query.WithAll<DroneTag, PathFailed>();
    }

    protected override void OnUpdate()
    {
        if (!m_addRngQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<DroneTag>().WithNone<RandomState>().WithStoreEntityQueryInField(ref m_addRngQuery).ForEach((Entity e, int entityInQueryIndex) =>
            {
                cmdBuffer.AddComponent(entityInQueryIndex, e, new RandomState((uint)e.Index + 1));
            }).ScheduleParallel();
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        if (!m_failedQuery.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent(m_failedQuery, m_failRemoveComponents);
        }

        if (!m_selectJobQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer();
            uint mask = JobMask;
            Entities.WithAll<DroneTag>().WithNone<WorkClearRocks, WorkPlantSeeds, WorkSellPlants>().WithNone<WorkTillGround>().WithStoreEntityQueryInField(ref m_selectJobQuery).ForEach((Entity e, ref RandomState rng) =>
            {
                cmdBuffer.AddComponent<WorkSellPlants>(e);
            }).Schedule();
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }
}