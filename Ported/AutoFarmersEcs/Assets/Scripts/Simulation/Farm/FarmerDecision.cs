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
public class FarmerDecision : SystemBase
{
    static readonly ComponentTypes m_failRemoveComponents = new ComponentTypes(new ComponentType[] { typeof(PathFailed), typeof(PathData), typeof(WorkClearRocks), typeof(WorkPlantSeeds), typeof(WorkSellPlants), typeof(WorkTillGround) });

    EntityCommandBufferSystem m_cmdSystem;
    EntityQuery m_addRngQuery;
    EntityQuery m_selectJobQuery;
    EntityQuery m_failedQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_failedQuery = Query.WithAll<FarmerTag, PathFailed>();
    }

    protected override void OnUpdate()
    {
        if (!m_addRngQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag>().WithNone<RandomState>().WithStoreEntityQueryInField(ref m_addRngQuery).ForEach((Entity e, int entityInQueryIndex) =>
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
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            Entities.WithAll<FarmerTag>().WithNone<WorkClearRocks, WorkPlantSeeds, WorkSellPlants>().WithNone<WorkTillGround>().WithStoreEntityQueryInField(ref m_selectJobQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng) =>
            {
                int rand = rng.Rng.NextInt(0, 4);
                if (rand == 0)
                {
                    cmdBuffer.AddComponent<WorkClearRocks>(entityInQueryIndex, e);
                }
                else if (rand == 1)
                {
                    cmdBuffer.AddComponent<WorkTillGround>(entityInQueryIndex, e);
                }
                //else if (rand == 2)
                //{
                //    cmdBuffer.AddComponent<WorkPlantSeeds>(e);
                //}
                //else if (rand == 3)
                //{
                //    cmdBuffer.AddComponent<WorkSellPlants>(e);
                //}
            }).ScheduleParallel();
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }
}