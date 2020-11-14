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

// PATHFINDING
// 1) Rock, Store, Normal ground, Tilled ground (no plant), Grown plant
// 2) Around rocks (farmer), over rocks (drone)
// 3) Tile states: empty, rock, tilled, plant

// PATHFINDING GOALS
// 1) Find closest reachable target (distance by path, not by world distance)
// 2) Go through walkable neighbors, A*
// 3) Must know whether tile is walkable
// 4) Must know whether tile is possible target

// IMPLEMENTATION
// 1) Walkable bits - bit field, one bit per agent type
// 2) Lookup for tile contents - main object type index + 6 object bits + 2 ground bits
//    - TypeManager.GetTypeIndex<RockTag>();

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class Pathfinding : SystemBase
{
    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        var mapSize = this.GetSettings().MapSize;
        var ground = GetSingletonEntity<Ground>();
        var lookup = GetSingletonEntity<LookupData>();
        int nonWalkableComponentIndex = TypeManager.GetTypeIndex<RockTag>();
        BufferFromEntity<Ground> groundArray = GetBufferFromEntity<Ground>(true);
        BufferFromEntity<LookupData> lookupDataArray = GetBufferFromEntity<LookupData>(true);
        BufferFromEntity<LookupEntity> lookupEntityArray = GetBufferFromEntity<LookupEntity>(true);

        Entities.WithReadOnly(lookupDataArray).WithReadOnly(groundArray).WithNone<PathData>().ForEach((Entity e, int entityInQueryIndex, in FindPath search, in Position position) =>
        {
            var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
            PathHelper.FindPath(groundArray[ground], lookupDataArray[lookup], (int2)position.Value, mapSize, nonWalkableComponentIndex, search, buffer);
            if (buffer.Length > 0)
            {
                var targetPos = (int2)buffer[0].Position;
                var target = lookupEntityArray[lookup][targetPos.x + targetPos.y * mapSize.x].Entity;
                cmdBuffer.AddComponent(entityInQueryIndex, e, new PathTarget { Entity = target });
            }
            else
            {
                cmdBuffer.AddComponent<PathFailed>(entityInQueryIndex, e);
            }
            cmdBuffer.RemoveComponent<FindPath>(entityInQueryIndex, e);
        }).Schedule();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}