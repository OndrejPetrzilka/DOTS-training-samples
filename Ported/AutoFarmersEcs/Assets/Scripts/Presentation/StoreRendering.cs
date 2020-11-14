using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class StoreRendering : SystemBase
{
    protected override void OnUpdate()
    {
        var settings = this.GetRenderSettings();
        var mesh = settings.storeMesh;
        var material = settings.storeMaterial;

        Entities.WithoutBurst().WithAll<StoreTag>().ForEach((Entity entity, in Position position) =>
        {
            var matrix = Matrix4x4.TRS(new Vector3(position.Value.x + .5f, .6f, position.Value.y + .5f), Quaternion.identity, new Vector3(1f, .6f, 1f));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
