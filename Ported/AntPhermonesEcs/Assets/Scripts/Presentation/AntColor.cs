using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(AntRendering))]
public class AntColor : JobComponentSystem
{
    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var carryColor = (float4)(Vector4)m_settings.carryColor;
        var searchColor = (float4)(Vector4)m_settings.searchColor;

        var carry = Entities.WithAll<AntTag, HoldingResourceTag>().ForEach((ref Brightness brightness, ref ColorData color) =>
        {
            color.Value += (carryColor * brightness.Value - color.Value) * 0.05f;
        }).Schedule(inputDeps);

        return Entities.WithAll<AntTag>().WithNone<HoldingResourceTag>().ForEach((ref Brightness brightness, ref ColorData color) =>
        {
            color.Value += (searchColor * brightness.Value - color.Value) * 0.05f;
        }).Schedule(carry);
    }
}
