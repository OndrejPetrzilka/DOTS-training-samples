using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerDecision : SystemBase
{
    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer();

        Entities.WithAll<FarmerTag>().WithNone<RandomState>().ForEach((Entity e, int entityInQueryIndex) =>
        {
            cmdBuffer.AddComponent(e, new RandomState((uint)e.Index + 1));
        }).Schedule();

        cmdBuffer = m_cmdSystem.CreateCommandBuffer();

        Entities.WithAll<FarmerTag>().WithNone<WorkClearRocks, WorkPlantSeeds, WorkSellPlants>().WithNone<WorkTillGround>().ForEach((Entity e, ref RandomState rng) =>
        {
            int rand = rng.Rng.NextInt(0, 4);
            if (rand == 0)
            {
                cmdBuffer.AddComponent<WorkClearRocks>(e);
            }
            //else if (rand == 1)
            //{
            //    cmdBuffer.AddComponent<WorkTillGround>(e);
            //}
            //else if (rand == 2)
            //{
            //    cmdBuffer.AddComponent<WorkPlantSeeds>(e);
            //}
            //else if (rand == 3)
            //{
            //    cmdBuffer.AddComponent<WorkSellPlants>(e);
            //}
        }).Schedule();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}