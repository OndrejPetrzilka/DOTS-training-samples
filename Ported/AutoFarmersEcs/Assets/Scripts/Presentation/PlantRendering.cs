using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class PlantRendering : SystemBase
{
    Dictionary<int, Mesh> m_plants = new Dictionary<int, Mesh>(32);

    RenderSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
    }

    public Mesh GetPlant(int seed)
    {
        if (!m_plants.TryGetValue(seed, out Mesh mesh))
        {
            mesh = PlantGenerator.GenerateMesh(seed);
            m_plants[seed] = mesh;
        }
        return mesh;
    }

    protected override void OnUpdate()
    {
        var material = m_settings.plantMaterial;

        material.SetFloat("_Growth", 1);

        Entities.WithoutBurst().ForEach((Entity entity, in PlantTag plant, in Position position) =>
        {
            Vector3 worldPos = new Vector3(position.Value.x + .5f, 0f, position.Value.y + .5f);
            //rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.value * 360f, Random.Range(-5f, 5f));

            float t = math.sqrt(plant.Growth);
            float3 scale = default;
            scale.y = t;
            scale.xz = math.smoothstep(0, 1, t * t * t * t * t) * .9f + .1f;

            var matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
            Graphics.DrawMesh(GetPlant(plant.Seed), matrix, material, 0);
        }).Run();
    }
}
