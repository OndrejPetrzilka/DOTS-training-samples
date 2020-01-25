using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AntColor : ComponentSystem
{
    AntSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
    }

    protected override void OnUpdate()
    {
        Entities.WithAllReadOnly<AntTag, HoldingResourceTag>().ForEach((ref Brightness brightness, ref ColorData color) =>
        {
            color.Value += ((float4)(Vector4)m_settings.carryColor * brightness.Value - color.Value) * 0.05f;
        });

        Entities.WithAllReadOnly<AntTag>().WithNone<HoldingResourceTag>().ForEach((ref Brightness brightness, ref ColorData color) =>
        {
            color.Value += ((float4)(Vector4)m_settings.searchColor * brightness.Value - color.Value) * 0.05f;
        });
    }
}
