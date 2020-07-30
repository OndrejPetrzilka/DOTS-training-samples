using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntResourceTarget : ComponentSystem
{
    struct HasResourceTargetTag : ISystemStateComponentData
    {
    }

    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
    }

    protected override void OnUpdate()
    {
        var resourcePosition = GetSingleton<Resource>().Position;

        // Has no target
        Entities.WithAll<AntTag>().WithNone<TargetPosition>().ForEach((Entity e) =>
        {
            EntityManager.AddComponentData(e, new TargetPosition { Value = resourcePosition });
        });

        // Picked up resource
        Entities.WithAll<AntTag, HoldingResourceTag>().WithNone<HasResourceTargetTag>().ForEach((Entity e, ref TargetPosition target) =>
        {
            target.Value = m_settings.colonyPosition;
            EntityManager.AddComponent<HasResourceTargetTag>(e);
        });

        // Dropped resource
        Entities.WithAll<AntTag, HasResourceTargetTag>().WithNone<HoldingResourceTag>().ForEach((Entity e, ref TargetPosition target) =>
        {
            target.Value = resourcePosition;
            EntityManager.RemoveComponent<HasResourceTargetTag>(e);
        });
    }
}
