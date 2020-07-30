using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(AntRendering))]
public class ObstacleRendering : ComponentSystem
{
    AntSettings m_settings;
    AntSettingsData m_settingsData;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_settingsData = AntSettingsManager.CurrentData;
    }

    protected override void OnUpdate()
    {
        Entities.WithAllReadOnly<ObstacleTag, Position, Radius>().ForEach((ref Position position, ref Radius radius) =>
        {
            Matrix4x4 matrix = Matrix4x4.TRS(((Vector2)position.Value) / m_settingsData.mapSize, Quaternion.identity, new Vector3(radius.Value * 2f, radius.Value * 2f, 1f) / m_settingsData.mapSize);
            Graphics.DrawMesh(m_settings.obstacleMesh, matrix, m_settings.obstacleMaterial, 0);
        });
    }
}
