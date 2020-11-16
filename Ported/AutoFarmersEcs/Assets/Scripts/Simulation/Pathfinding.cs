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

/// <summary>
/// Inputs: FindPath, Position, ~PathData
/// Outputs: 
///     ~FindPath, PathData, PathTarget (success)
///     ~FindPath, PathData, PathFailed (failure)
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class Pathfinding : SystemBase
{
    static readonly ComponentTypes m_pathFailedRemove = new ComponentTypes(typeof(FindPath), typeof(PathData));
    static readonly int m_nonWalkableComponent = TypeManager.GetTypeIndex<RockTag>();

    EntityCommandBufferSystem m_cmdSystem;
    EntityQuery m_needsPathQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        RequireForUpdate(m_needsPathQuery);
    }

    protected override void OnUpdate()
    {
        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        var mapSize = Settings.MapSize;
        var ground = GetSingletonEntity<Ground>();
        var lookup = GetSingletonEntity<LookupData>();
        var pathFailedRemove = m_pathFailedRemove;
        int nonWalkableComponentIndex = m_nonWalkableComponent;
        BufferFromEntity<Ground> groundArray = GetBufferFromEntity<Ground>(true);
        BufferFromEntity<LookupData> lookupDataArray = GetBufferFromEntity<LookupData>(true);
        BufferFromEntity<LookupEntity> lookupEntityArray = GetBufferFromEntity<LookupEntity>(true);

        Entities.WithReadOnly(lookupEntityArray).WithReadOnly(lookupDataArray).WithReadOnly(groundArray).WithNone<PathData>().WithStoreEntityQueryInField(ref m_needsPathQuery).ForEach((Entity e, int entityInQueryIndex, in FindPath search, in Position position) =>
        {
            var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
            PathHelper.FindPath(groundArray[ground], lookupDataArray[lookup], (int2)position.Value, mapSize, nonWalkableComponentIndex, search, buffer);
            if (buffer.Length > 0)
            {
                var targetPos = (int2)buffer[0].Position;
                var target = lookupEntityArray[lookup][targetPos.x + targetPos.y * mapSize.x].Entity;
                cmdBuffer.AddComponent(entityInQueryIndex, e, new PathTarget { Entity = target });
                cmdBuffer.RemoveComponent<FindPath>(entityInQueryIndex, e);
            }
            else
            {
                cmdBuffer.AddComponent<PathFailed>(entityInQueryIndex, e);
                cmdBuffer.RemoveComponent(entityInQueryIndex, e, pathFailedRemove);
            }
        }).ScheduleParallel();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}