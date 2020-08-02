using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class RockRendering : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mesh = settings.rockMesh;
        var material = settings.rockMaterial;

        Entities.WithoutBurst().WithAll<RockTag>().ForEach((Entity entity, in Position position, in Size size, in Depth depth) =>
        {
            RectInt rect = new RectInt(position.Value.x, position.Value.y, size.Value.x, size.Value.y);
            Vector2 center2D = rect.center;
            Vector3 worldPos = new Vector3(center2D.x + .5f, depth.Value * .5f, center2D.y + .5f);
            Vector3 scale = new Vector3(rect.width + .5f, depth.Value, rect.height + .5f);
            var matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }).Run();
    }
}
