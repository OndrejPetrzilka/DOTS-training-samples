using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntResourceTarget : SystemBase
{
    AntSettingsData m_settings;
    EndSimulationEntityCommandBufferSystem m_endSimulation;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
        m_endSimulation = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();

        var resourcePosition = GetSingleton<Resource>().Position;

        Entities.WithAll<AntTag>().WithNone<TargetPosition>().WithStructuralChanges().ForEach((Entity e, int entityInQueryIndex) =>
        {
            EntityManager.AddComponentData(e, new TargetPosition { Value = resourcePosition });
        }).Run();
    }

    protected override void OnUpdate()
    {
    }
}
