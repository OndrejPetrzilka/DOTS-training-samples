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
    RenderSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
    }

    protected override void OnUpdate()
    {
        var mesh = m_settings.storeMesh;
        var material = m_settings.storeMaterial;

        Entities.WithoutBurst().WithAll<StoreTag>().ForEach((Entity entity, in Position position) =>
        {
            var matrix = Matrix4x4.TRS(new Vector3(position.Value.x + .5f, .6f, position.Value.y + .5f), Quaternion.identity, new Vector3(1f, .6f, 1f));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
