using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FollowPath : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        float walkSpeed = 4;
        float deltaTime = Time.fixedDeltaTime;

        Entities.WithStructuralChanges().ForEach((Entity e, DynamicBuffer<PathData> path, ref Position position) =>
        {
            int2 pos = (int2)math.floor(position.Value);
            if (path.Length == 0)
            {
                EntityManager.RemoveComponent<PathData>(e);
            }
            else if (math.all(path[path.Length - 1].Position == pos))
            {
                path.RemoveAt(path.Length - 1);
            }
            else
            {
                //if (Farm.IsBlocked(nextTileX, nextTileY) == false)
                {
                    float offset = .5f;
                    //if (Farm.groundStates[nextTileX, nextTileY] == GroundState.Plant)
                    //{
                    //    offset = .01f;
                    //}
                    float2 targetPos = path[path.Length - 1].Position + new float2(offset);
                    position.Value = Vector2.MoveTowards(position.Value, targetPos, walkSpeed * deltaTime);
                }
            }
        }).Run();
    }
}