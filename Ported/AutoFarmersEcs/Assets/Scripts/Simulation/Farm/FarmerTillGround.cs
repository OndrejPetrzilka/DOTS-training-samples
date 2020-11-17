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

[UpdateInGroup(typeof(FarmGroup))]
public class FarmerTillGround : SystemBase
{
    static readonly int m_rockIndex = TypeManager.GetTypeIndex<RockTag>();
    static readonly int m_storeIndex = TypeManager.GetTypeIndex<StoreTag>();
    static readonly ComponentTypes m_componentTypesRemoveWork = new ComponentTypes(typeof(WorkTillGround), typeof(WorkTarget));
    static readonly ComponentTypes m_finishedWorkComponents = new ComponentTypes(new ComponentType[] { typeof(WorkTillGround), typeof(TillingZone), typeof(WorkTarget), typeof(PathFinished), typeof(PathData), typeof(PathTarget) });

    EntityCommandBufferSystem m_cmdSystem;
    EntityQuery m_removedWork;
    EntityQuery m_findZoneQuery;
    EntityQuery m_tileQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_removedWork = Query.WithAll<TillingZone>().WithNone<WorkTillGround>();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;

        // Remove tilling zone when work is lost
        if (!m_removedWork.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent(m_removedWork, typeof(TillingZone));
        }

        if (!m_findZoneQuery.IsEmptyIgnoreFilter)
        {
            var ground = GetSingletonEntity<Ground>();
            var lookup = GetSingletonEntity<LookupData>();
            BufferFromEntity<Ground> groundArray = GetBufferFromEntity<Ground>(true);
            BufferFromEntity<LookupData> lookupDataArray = GetBufferFromEntity<LookupData>(true);
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var componentTypesRemoveWork = m_componentTypesRemoveWork;
            int rockIndex = m_rockIndex;
            int storeIndex = m_storeIndex;

            // Initial state, assign tilling zone
            Entities.WithReadOnly(groundArray).WithReadOnly(lookupDataArray).WithAll<FarmerTag, WorkTillGround>().WithNone<TillingZone>().WithStoreEntityQueryInField(ref m_findZoneQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in Position position) =>
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
                        if (!hasTarget && !groundArray[ground][index].IsTilled)
                        {
                            hasTarget = true;
                        }
                        int componentIndex = lookupDataArray[lookup][index].ComponentTypeIndex;
                        if (componentIndex == rockIndex || componentIndex == storeIndex)
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
                else if (rng.Rng.NextFloat() < .2f)
                {
                    cmdBuffer.RemoveComponent(entityInQueryIndex, e, componentTypesRemoveWork);
                }
            }).ScheduleParallel();
            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Reached target
        if (!m_tileQuery.IsEmptyIgnoreFilter)
        {
            var finishedWorkComponents = m_finishedWorkComponents;
            var groundEntity = GetSingletonEntity<Ground>();
            var groundBuffer = GetBufferFromEntity<Ground>(false);
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

            Entities.WithAll<FarmerTag, WorkTillGround, PathFinished>().WithStoreEntityQueryInField(ref m_tileQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in TillingZone zone, in Position position) =>
            {
                var ground = groundBuffer[groundEntity];
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
                    var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
                    buffer.Length = 0;
                    buffer.Add(new PathData { Position = newTile });

                    cmdBuffer.RemoveComponent<PathFinished>(entityInQueryIndex, e);
                }
                else
                {
                    cmdBuffer.RemoveComponent(entityInQueryIndex, e, finishedWorkComponents);
                }
            }).Schedule();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
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