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
    NativeArray<Matrix4x4> m_rotationMatrices;
    MaterialPropertyBlock m_block;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;

        m_rotationMatrices = new NativeArray<Matrix4x4>(m_settings.rotationResolution, Allocator.Persistent);
        for (int i = 0; i < m_settings.rotationResolution; i++)
        {
            float angle = (float)i / m_settings.rotationResolution;
            angle *= 360f;
            m_rotationMatrices[i] = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, angle), m_settings.antSize);
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
        Entities.WithAllReadOnly<AntTag, Position, FacingAngle, ColorData>().ForEach((Entity e, ref Position position, ref FacingAngle facingAngle, ref ColorData color) =>
        {
            float angle = facingAngle.Value;
            angle /= Mathf.PI * 2f;
            angle -= Mathf.Floor(angle);
            angle *= m_settings.rotationResolution;

            Matrix4x4 matrix = m_rotationMatrices[((int)angle) % m_settings.rotationResolution];
            matrix.m03 = position.Value.x / m_settings.mapSize;
            matrix.m13 = position.Value.y / m_settings.mapSize;

            m_block.SetVector("_Color", color.Value);
            Graphics.DrawMesh(m_settings.antMesh, matrix, m_settings.antMaterial, 0, null, 0, m_block);
        });
    }
}
