using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class AntRendering : ComponentSystem
{
    AntSettings m_settings;
    AntSettingsData m_settingsData;
    NativeArray<Matrix4x4> m_rotationMatrices;
    MaterialPropertyBlock m_block;

    List<Matrix4x4> m_matrices = new List<Matrix4x4>(1024);
    List<Vector4> m_colors = new List<Vector4>(1024);

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_settingsData = AntSettingsManager.CurrentData;

        m_rotationMatrices = new NativeArray<Matrix4x4>(m_settingsData.rotationResolution, Allocator.Persistent);
        for (int i = 0; i < m_settingsData.rotationResolution; i++)
        {
            float angle = (float)i / m_settingsData.rotationResolution;
            angle *= 360f;
            m_rotationMatrices[i] = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angle), m_settingsData.antSize);
        }

        m_block = new MaterialPropertyBlock();
    }

    protected override void OnDestroy()
    {
        m_rotationMatrices.Dispose();
        m_rotationMatrices = default;
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        m_matrices.Clear();
        m_colors.Clear();

        Entities.WithAllReadOnly<AntTag, Position, FacingAngle, ColorData>().ForEach((Entity e, ref Position position, ref FacingAngle facingAngle, ref ColorData color) =>
        {
            float angle = facingAngle.Value;
            angle /= Mathf.PI * 2f;
            angle -= Mathf.Floor(angle);
            angle *= m_settingsData.rotationResolution;

            Matrix4x4 matrix = m_rotationMatrices[((int)angle) % m_settingsData.rotationResolution];
            matrix.m03 = position.Value.x / m_settingsData.mapSize;
            matrix.m13 = position.Value.y / m_settingsData.mapSize;

            m_matrices.Add(matrix);
            m_colors.Add(color.Value);
        });

        m_block.SetVectorArray("_Color", m_colors);
        Graphics.DrawMeshInstanced(m_settings.antMesh, 0, m_settings.antMaterial, m_matrices, m_block);
    }
}
