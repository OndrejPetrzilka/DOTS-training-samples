using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class AntRendering : SystemBase
{
    AntSettings m_settings;
    AntSettingsData m_settingsData;
    NativeArray<Matrix4x4> m_rotationMatrices;
    MaterialPropertyBlock m_block;

    Matrix4x4[] m_matrices = new Matrix4x4[1023];
    Vector4[] m_colors = new Vector4[1023];

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

    protected unsafe override void OnUpdate()
    {
        int count = 0;
        var rotations = m_rotationMatrices;
        var settings = m_settingsData;

        fixed (Matrix4x4* matrixPtr = m_matrices)
        fixed (Vector4* colorPtr = m_colors)
        {
            var matrices = matrixPtr;
            var colors = colorPtr;
            var countPtr = &count;

            Entities.WithNativeDisableUnsafePtrRestriction(countPtr).WithNativeDisableUnsafePtrRestriction(matrices).WithNativeDisableUnsafePtrRestriction(colors).WithAll<AntTag>().ForEach((Entity e, in Position position, in FacingAngle facingAngle, in ColorData color) =>
            {
                float angle = facingAngle.Value;
                angle /= Mathf.PI * 2f;
                angle -= Mathf.Floor(angle);
                angle *= settings.rotationResolution;

                Matrix4x4 matrix = rotations[((int)angle) % settings.rotationResolution];
                matrix.m03 = position.Value.x / settings.mapSize;
                matrix.m13 = position.Value.y / settings.mapSize;

                matrices[*countPtr] = matrix;
                colors[*countPtr] = color.Value;
                (*countPtr)++;
            }).ScheduleParallel(Dependency).Complete();
        }

        m_block.SetVectorArray("_Color", m_colors);
        Graphics.DrawMeshInstanced(m_settings.antMesh, 0, m_settings.antMaterial, m_matrices, count, m_block);
    }
}
