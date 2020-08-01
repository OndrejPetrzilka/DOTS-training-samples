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

    List<Matrix4x4> m_matrices = new List<Matrix4x4>();

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_settingsData = AntSettingsManager.CurrentData;
        m_matrices.Clear();

        Entities.WithAllReadOnly<ObstacleTag, Position, Radius>().ForEach((ref Position position, ref Radius radius) =>
        {
            Matrix4x4 matrix = Matrix4x4.TRS(((Vector2)position.Value) / m_settingsData.mapSize, Quaternion.identity, new Vector3(radius.Value * 2f, radius.Value * 2f, 1f) / m_settingsData.mapSize);
            m_matrices.Add(matrix);
        });
    }

    protected override void OnUpdate()
    {
        Graphics.DrawMeshInstanced(m_settings.obstacleMesh, 0, m_settings.obstacleMaterial, m_matrices);
    }
}
