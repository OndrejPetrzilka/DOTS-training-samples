using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerDecision : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        Entities.WithStructuralChanges().WithAll<FarmerTag>().WithNone<WorkClearRocks, WorkPlantSeeds, WorkSellPlants>().WithNone<WorkTillGround>().ForEach((Entity e) =>
        {
            int rand = Random.Range(0, 4);
            if (rand == 0)
            {
                EntityManager.AddComponent<WorkClearRocks>(e);
            }
            else if (rand == 1)
            {
                EntityManager.AddComponent<WorkTillGround>(e);
            }
            else if (rand == 2)
            {
                EntityManager.AddComponent<WorkPlantSeeds>(e);
            }
            else if (rand == 3)
            {
                //EntityManager.AddComponent<WorkSellPlants>(e);
            }
        }).Run();
    }
}