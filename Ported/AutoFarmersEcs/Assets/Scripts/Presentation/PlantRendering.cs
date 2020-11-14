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

    protected override void OnUpdate()
    {
        var settings = this.GetRenderSettings();
        var material = settings.plantMaterial;

        material.SetFloat("_Growth", 1);

        Entities.WithoutBurst().ForEach((Entity entity, in PlantTag plant, in Position position) =>
        {
            Vector3 worldPos = new Vector3(position.Value.x + .5f, 0f, position.Value.y + .5f);
            //rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.value * 360f, Random.Range(-5f, 5f));

            if (!m_plants.TryGetValue(plant.Seed, out Mesh mesh))
            {
                mesh = PlantGenerator.GenerateMesh(plant.Seed);
                m_plants[plant.Seed] = mesh;
            }

            float t = math.sqrt(plant.Growth);
            float3 scale = default;
            scale.y = t;
            scale.xz = math.smoothstep(0, 1, t * t * t * t * t) * .9f + .1f;

            var matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
