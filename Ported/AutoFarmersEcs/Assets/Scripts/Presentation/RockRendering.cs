using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class RockRendering : SystemBase
{
    RenderSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
    }

    protected override void OnUpdate()
    {
        var mesh = m_settings.rockMesh;
        var material = m_settings.rockMaterial;

        Entities.WithoutBurst().WithAll<RockTag>().ForEach((Entity entity, in Position position, in Size size, in Health health, in Depth depth) =>
        {
            float maxHealth = (size.Value.x + 1) * (size.Value.y + 1) * 15;
            float2 center2D = position.Value + size.Value * 0.5f;
            float3 worldPos = new float3(center2D.x + .5f, depth.Value * .5f, center2D.y + .5f);
            float3 scale = new float3(size.Value.x + .5f, depth.Value * health.Value / maxHealth, size.Value.y + .5f);
            var matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
