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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerClearRocks : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;
        int count = 0;

        NativeArray<Entity> lookup = new NativeArray<Entity>(mapSize.x * mapSize.y, Allocator.Temp, NativeArrayOptions.ClearMemory);

        Entities.WithAll<RockTag>().ForEach((Entity e, in Position pos, in Size size) =>
        {
            count++;
            for (int x = 0; x <= size.Value.x; x++)
            {
                for (int y = 0; y <= size.Value.y; y++)
                {
                    int2 p = pos.Value + new int2(x, y);
                    int index = p.x + p.y * mapSize.x;
                    lookup[index] = e;
                }
            }
        }).Run();

        // Initial state
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkClearRocks>().WithNone<WorkTarget>().ForEach((Entity e) =>
        {
            // Find rock & generate path
            // Do width-first search for a rock, remember the path on the way

            if (count == 0)
            {
                // TODO: Separate job
                EntityManager.RemoveComponent<WorkClearRocks>(e);
            }
            else
            {
                var rock = lookup.First(s => s != Entity.Null);
                EntityManager.AddComponentData(e, new WorkTarget { Value = rock });
                var buffer = EntityManager.AddBuffer<PathData>(e);
                buffer.Add(new PathData { Position = EntityManager.GetComponentData<Position>(rock).Value });
            }

        }).Run();

        lookup.Dispose();

        // Reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkClearRocks>().WithNone<PathData>().ForEach((Entity e, in WorkTarget target) =>
        {
            // Attack rock
            int health = 0;
            if (target.Value != Entity.Null)
            {
                health = EntityManager.GetComponentData<Health>(target.Value).Value - 1;
                EntityManager.SetComponentData(target.Value, new Health { Value = health });
                if (health <= 0)
                {
                    EntityManager.DestroyEntity(target.Value);
                }
            }
            if (health <= 0)
            {
                EntityManager.RemoveComponent<WorkClearRocks>(e);
                EntityManager.RemoveComponent<WorkTarget>(e);
            }
        }).Run();
    }
}