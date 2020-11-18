using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FarmGroup))]
public class PlantGrowth : SystemBase
{
    EntityCommandBufferSystem m_cmdSystem;
    EntityQuery m_query;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
        m_query = Query.WithAll<PlantTag>().WithNone<LookupComponentFilters>();
    }

    protected override void OnUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (!m_query.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().AddComponent<LookupComponentFilters>(m_query);
        }

        Entities.ForEach((ref PlantTag plant, ref LookupComponentFilters filters) =>
        {
            plant.Growth = Mathf.Min(plant.Growth + deltaTime / 10f, 1f);
            filters.Value = plant.Growth == 1 ? (byte)1 : (byte)0;
        }).ScheduleParallel();
    }
}
