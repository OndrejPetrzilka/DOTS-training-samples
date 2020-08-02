using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class GroundRendering : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<Ground>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mesh = settings.groundMesh;
        var material = settings.groundMaterial;

        var rng = new Random(1);

        var ground = GetBuffer<Ground>(GetSingletonEntity<Ground>());
        for (int x = 0; x < settings.mapSize.x; x++)
        {
            for (int y = 0; y < settings.mapSize.y; y++)
            {
                var item = ground[x + y * settings.mapSize.x];

                Vector3 pos = new Vector3(x + .5f, 0f, y + .5f);
                float zRot = rng.NextInt(0, 2) * 180f;
                var matrix = Matrix4x4.TRS(pos, Quaternion.Euler(90f, 0f, zRot), Vector3.one);

                material.SetFloat("_Tilled", item.Till);
                //groundMatProps[i].SetFloatArray("_Tilled", tilledProperties[i]);

                Graphics.DrawMesh(mesh, matrix, material, 0);
            }
        }

        //Entities.WithoutBurst().WithAll<StoreTag>().ForEach((Entity entity, in Position position) =>
        //{
        //    var matrix = Matrix4x4.TRS(new Vector3(position.Value.x + .5f, .6f, position.Value.y + .5f), Quaternion.identity, new Vector3(1f, .6f, 1f));
        //    Graphics.DrawMesh(mesh, matrix, material, 0);
        //}).Run();
    }
}
