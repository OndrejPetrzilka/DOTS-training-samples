﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

// PATHFINDING
// 1) Closest store, closest rock, closest unreserved grown plant, closest empty field
// 2) Around rocks (farmer), over rocks (drone)
// 3) Tile states: empty, rock, tilled, plant

// PATHFINDING GOALS
// 1) Find closest reachable target (distance by path, not by world distance)
// 2) Go through walkable neighbors, A*
// 3) Must know whether tile is walkable
// 4) Must know whether tile is possible target

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class Pathfinding : SystemBase
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
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.WithNone<PathData>().ForEach((Entity e, int entityInQueryIndex, in FindPath search, in Position position) =>
        {
            
        }).ScheduleParallel();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}