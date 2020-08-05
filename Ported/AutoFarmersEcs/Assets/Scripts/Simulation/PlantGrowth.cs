using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class PlantGrowth : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        Entities.ForEach((Entity e, ref PlantTag plant) =>
        {
            plant.Growth = Mathf.Min(plant.Growth + deltaTime / 10f, 1f);
        }).Run();
    }
}
