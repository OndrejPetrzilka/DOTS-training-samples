using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerTillGround : SystemBase
{
    EntityQuery m_removedWork;

    EntityCommandBufferSystem m_cmdSystem;
    ComponentTypes m_finishedWorkComponents = new ComponentTypes(new ComponentType[] { typeof(WorkTillGround), typeof(TillingZone), typeof(WorkTarget), typeof(PathFinished), typeof(PathData), typeof(PathTarget) });

    protected override void OnCreate()
    {
        base.OnCreate();

        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_removedWork = Query.WithAll<TillingZone>().WithNone<WorkTillGround>();

        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<Ground>();
        RequireSingletonForUpdate<RockLookup>();
        RequireSingletonForUpdate<StoreLookup>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;

        var rocks = this.GetSingleton<RockLookup>();
        var stores = this.GetSingleton<StoreLookup>();
        var ground = GetBuffer<Ground>(GetSingletonEntity<Ground>());

        // Remove tilling zone when work is lost
        EntityManager.RemoveComponent(m_removedWork, typeof(TillingZone));

        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        // Initial state, assign tilling zone
        var componentTypesRemoveWork = new ComponentTypes(typeof(WorkTillGround), typeof(WorkTarget));
        Entities.WithAll<FarmerTag, WorkTillGround>().WithNone<TillingZone>().ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in Position position) =>
        {
            // Find area & generate path
            int2 tile = (int2)math.floor(position.Value);

            int width = rng.Rng.NextInt(1, 8);
            int height = rng.Rng.NextInt(1, 8);
            int minX = tile.x + rng.Rng.NextInt(-10, 10 - width);
            int minY = tile.y + rng.Rng.NextInt(-10, 10 - height);
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (minX + width >= mapSize.x) minX = mapSize.x - 1 - width;
            if (minY + height >= mapSize.y) minY = mapSize.y - 1 - height;

            bool blocked = false;
            bool hasTarget = false;
            for (int x = minX; x <= minX + width; x++)
            {
                for (int y = minY; y <= minY + height; y++)
                {
                    int index = x + y * mapSize.x;
                    if (!hasTarget && !ground[index].IsTilled)
                    {
                        hasTarget = true;
                    }
                    if (rocks[index].Entity != Entity.Null || stores[index].Entity != Entity.Null)
                    {
                        blocked = true;
                        break;
                    }
                }
            }
            if (!blocked && hasTarget)
            {
                cmdBuffer.AddComponent(entityInQueryIndex, e, new TillingZone() { Position = new int2(minX, minY), Size = new int2(width, height) });
                var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
                buffer.Add(new PathData { Position = new int2(minX, minY) });
            }
            else
            {
                if (rng.Rng.NextFloat() < .2f)
                {
                    cmdBuffer.RemoveComponent(entityInQueryIndex, e, componentTypesRemoveWork);
                }
            }
        }).Schedule();

        // Reached target
        var cmdBuffer2 = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
        var finishedWorkComponents = m_finishedWorkComponents;

        Entities.WithAll<FarmerTag, WorkTillGround, PathFinished>().ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in TillingZone zone, in Position position) =>
        {
            var min = zone.Position;
            var max = zone.Position + zone.Size;

            //Debug.DrawLine(new Vector3(min.x, .1f, min.y), new Vector3(max.x + 1f, .1f, min.y), Color.green);
            //Debug.DrawLine(new Vector3(max.x + 1f, .1f, min.y), new Vector3(max.x + 1f, .1f, max.y + 1f), Color.green);
            //Debug.DrawLine(new Vector3(max.x + 1f, .1f, max.y + 1f), new Vector3(min.x, .1f, max.y + 1f), Color.green);
            //Debug.DrawLine(new Vector3(min.x, .1f, max.y + 1f), new Vector3(min.x, .1f, min.y), Color.green);

            // Till ground
            int2 tile = (int2)math.floor(position.Value);
            int index = tile.x + tile.y * mapSize.x;

            if (!ground[index].IsTilled)
            {
                ground[index] = new Ground() { Till = rng.Rng.NextFloat(0.8f, 1) };
            }

            if (TryFindNextTile(zone, mapSize, ground, out int2 newTile))
            {
                var buffer = cmdBuffer2.AddBuffer<PathData>(entityInQueryIndex, e);
                buffer.Length = 0;
                buffer.Add(new PathData { Position = newTile });

                cmdBuffer2.RemoveComponent<PathFinished>(entityInQueryIndex, e);
            }
            else
            {
                cmdBuffer2.RemoveComponent(entityInQueryIndex, e, finishedWorkComponents);
            }
        }).Schedule();

        Dependency.Complete();
    }

    private static bool TryFindNextTile(TillingZone zone, int2 mapSize, DynamicBuffer<Ground> ground, out int2 result)
    {
        // Find path to next tile
        int2 end = zone.Position + zone.Size;
        for (int x = zone.Position.x; x <= end.x; x++)
        {
            for (int y = zone.Position.y; y <= end.y; y++)
            {
                int newIndex = x + y * mapSize.x;
                if (!ground[newIndex].IsTilled)
                {
                    result = new int2(x, y);
                    return true;
                }
            }
        }
        result = default;
        return false;
    }
}