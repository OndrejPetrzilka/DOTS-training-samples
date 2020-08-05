using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerMovement : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        var moveSmooth = 1f - Mathf.Pow(this.GetSettings().movementSmooth, Time.fixedDeltaTime);

        Entities.ForEach((Entity e, ref SmoothPosition smoothPosition, in Position position) =>
        {
            smoothPosition.Value = math.lerp(smoothPosition.Value, position.Value, moveSmooth);
        }).Run();
    }
}
