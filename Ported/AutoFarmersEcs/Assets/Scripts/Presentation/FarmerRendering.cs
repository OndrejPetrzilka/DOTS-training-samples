using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class FarmerRendering : SystemBase
{
    RenderSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
    }

    protected override void OnUpdate()
    {
        var mesh = m_settings.farmerMesh;
        var material = m_settings.farmerMaterial;

        Entities.WithoutBurst().WithAll<FarmerTag>().ForEach((Entity entity, in SmoothPosition smoothPosition) =>
        {
            var pos = smoothPosition.Value;
            pos += EntityManager.HasComponent<Offset>(entity) ? EntityManager.GetComponentData<Offset>(entity).Value : default;

            var matrix = Matrix4x4.Translate(new float3(pos.x, .5f, pos.y)) * Matrix4x4.Scale(Vector3.one * .5f);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
