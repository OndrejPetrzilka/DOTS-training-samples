using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class FarmerCarryPlantRendering : SystemBase
{
    RenderSettings m_settings;
    PlantRendering m_plantRendering;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
        m_plantRendering = World.GetOrCreateSystem<PlantRendering>();
    }

    protected override void OnUpdate()
    {
        var material = m_settings.plantMaterial;

        material.SetFloat("_Growth", 1);

        Entities.WithoutBurst().ForEach((Entity entity, in CarryingPlant plant, in Position pos, in SmoothPosition position) =>
        {
            const float height = 0.4f;
            const float dist = -0.4f;
            const float angle = 60;

            float2 dir = position.Value - pos.Value;
            float3 offset = default;
            offset.xz = math.normalizesafe(dir) * dist;

            Vector3 worldPos = new Vector3(position.Value.x + .5f, height, position.Value.y + .5f) + (Vector3)offset;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            var matrix = Matrix4x4.TRS(worldPos, rotation, Vector3.one);
            Graphics.DrawMesh(m_plantRendering.GetPlant(plant.Seed), matrix, material, 0);
        }).Run();
    }
}
