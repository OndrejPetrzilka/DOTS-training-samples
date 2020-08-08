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
    EntityQuery m_query;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();

        m_query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(PlantTag) },
            None = new ComponentType[] { typeof(LookupComponentFilters) },
        });
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        EntityManager.AddComponent<LookupComponentFilters>(m_query);

        Entities.ForEach((Entity e, ref PlantTag plant, ref LookupComponentFilters filters) =>
        {
            plant.Growth = Mathf.Min(plant.Growth + deltaTime / 10f, 1f);
            filters.Value = plant.Growth == 1 ? (byte)1 : (byte)0;
        }).ScheduleParallel();
    }
}
