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
    protected override void OnUpdate()
    {
        var settings = this.GetRenderSettings();
        var mesh = settings.rockMesh;
        var material = settings.rockMaterial;

        Entities.WithoutBurst().WithAll<RockTag>().ForEach((Entity entity, in Position position, in Size size, in Depth depth) =>
        {
            float2 center2D = position.Value + size.Value * 0.5f;
            float3 worldPos = new float3(center2D.x + .5f, depth.Value * .5f, center2D.y + .5f);
            float3 scale = new float3(size.Value.x + .5f, depth.Value, size.Value.y + .5f);
            var matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
