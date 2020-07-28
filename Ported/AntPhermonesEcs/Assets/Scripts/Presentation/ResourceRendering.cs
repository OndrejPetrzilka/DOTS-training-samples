using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(ObstacleRendering))]
public class ResourceRendering : ComponentSystem
{
    AntSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
    }

    protected override void OnUpdate()
    {
        var colonyMatrix = Matrix4x4.TRS(m_settings.colonyPosition / m_settings.mapSize, Quaternion.identity, new Vector3(4f, 4f, .1f) / m_settings.mapSize);

        Vector2 resourcePosition = GetSingleton<Resource>().Position;
        var resourceMatrix = Matrix4x4.TRS(resourcePosition / m_settings.mapSize, Quaternion.identity, new Vector3(4f, 4f, .1f) / m_settings.mapSize);

        Graphics.DrawMesh(m_settings.colonyMesh, colonyMatrix, m_settings.colonyMaterial, 0);
        Graphics.DrawMesh(m_settings.resourceMesh, resourceMatrix, m_settings.resourceMaterial, 0);
    }
}
