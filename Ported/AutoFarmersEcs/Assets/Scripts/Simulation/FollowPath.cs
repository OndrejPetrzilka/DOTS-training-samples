using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Inputs: PathData, Position, ~PathFinished
/// Outputs: PathFinished
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FollowPath : SystemBase
{
    EntityCommandBufferSystem m_cmdSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float walkSpeed = 4;
        float deltaTime = Time.fixedDeltaTime;

        var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.WithNone<PathFinished>().ForEach((Entity e, int entityInQueryIndex, DynamicBuffer<PathData> path, ref Position position) =>
        {
            int2 pos = (int2)math.floor(position.Value);
            if (path.Length == 0)
            {
                cmdBuffer.RemoveComponent<PathData>(entityInQueryIndex, e);
                cmdBuffer.AddComponent<PathFinished>(entityInQueryIndex, e);
            }
            else if (math.all(path[path.Length - 1].Position == pos))
            {
                if (path.Length == 1)
                {
                    cmdBuffer.RemoveComponent<PathData>(entityInQueryIndex, e);
                    cmdBuffer.AddComponent<PathFinished>(entityInQueryIndex, e);
                }
                else
                {
                    path.RemoveAt(path.Length - 1);
                }
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
        }).ScheduleParallel();

        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}