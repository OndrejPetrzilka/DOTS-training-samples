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
    EntityQuery m_hasWork;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<RockLookup>();
        m_hasWork = GetEntityQuery(typeof(WorkClearRocks));
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var lookup = this.GetSingleton<RockLookup>();
        var mapSize = settings.mapSize;

        bool remove = false;
        if (lookup.Length > 0)
        {
            // Initial state
            Entities.WithStructuralChanges().WithAll<FarmerTag, WorkClearRocks>().WithNone<WorkTarget>().ForEach((Entity e, in Position position) =>
            {
                // Find rock & generate path
                // TODO: Do width-first search for a rock, remember the path on the way

                float distSq = float.MaxValue;
                Entity rock = Entity.Null;
                for (int i = 0; i < lookup.Length; i++)
                {
                    int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                    float newDistSq = math.lengthsq(position.Value - pos);
                    if (newDistSq < distSq)
                    {
                        var newRock = lookup[i].Entity;
                        if (EntityManager.Exists(newRock))
                        {
                            rock = newRock;
                            distSq = newDistSq;
                        }
                    }
                }

                if (rock == Entity.Null)
                {
                    remove = true;
                }
                else
                {
                    EntityManager.AddComponentData(e, new WorkTarget { Value = rock });
                    var buffer = EntityManager.AddBuffer<PathData>(e);
                    buffer.Add(new PathData { Position = EntityManager.GetComponentData<Position>(rock).Value });
                }
            }).Run();

            if (remove)
            {
                EntityManager.RemoveComponent(m_hasWork, typeof(WorkClearRocks));
            }
        }

        // Reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkClearRocks>().WithNone<PathData>().ForEach((Entity e, in WorkTarget target) =>
        {
            // Attack rock
            var rockPosition = EntityManager.GetComponentData<Position>(target.Value).Value;

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
            
            var pos = EntityManager.GetComponentData<SmoothPosition>(e).Value;
            Offset offset = default;
            offset.Value = math.normalizesafe(rockPosition - pos) * 0.5f * Random.value;
            EntityManager.SetComponentData(e, offset);
            if (health <= 0)
            {
                EntityManager.RemoveComponent<WorkClearRocks>(e);
                EntityManager.RemoveComponent<WorkTarget>(e);
            }
        }).Run();
    }
}