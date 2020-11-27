using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class FarmerRendering : SystemBase
{
    RenderSettings m_settings;

    List<Matrix4x4[]> m_matrices = new List<Matrix4x4[]>(2);
    List<GCHandle> m_pins = new List<GCHandle>();
    EntityQuery m_query;

    const int BatchSize = 1023;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = this.GetRenderSettings();
    }

    protected unsafe override void OnUpdate()
    {
        var mesh = m_settings.farmerMesh;
        var material = m_settings.farmerMaterial;

        int entityCount = m_query.CalculateEntityCount();
        int batchCount = (entityCount - 1) / BatchSize + 1;

        int lastBatchSize = entityCount - entityCount / BatchSize * BatchSize;

        while (m_matrices.Count < entityCount)
        {
            m_matrices.Add(new Matrix4x4[BatchSize]);
        }

        NativeArray<IntPtr> pins = new NativeArray<IntPtr>(batchCount, Allocator.TempJob);

        for (int i = 0; i < batchCount; i++)
        {
            var handle = GCHandle.Alloc(m_matrices[i], GCHandleType.Pinned);
            m_pins.Add(handle);
            pins[i] = handle.AddrOfPinnedObject();
        }

        Entities.WithNativeDisableContainerSafetyRestriction(pins).WithAll<FarmerTag>().WithStoreEntityQueryInField(ref m_query).ForEach((Entity entity, int entityInQueryIndex, in Offset offset, in SmoothPosition smoothPosition) =>
        {
            var pos = smoothPosition.Value + offset.Value;
            var matrix = float4x4.TRS(new float3(pos.x, .5f, pos.y), quaternion.identity, new float3(0.5f, 0.5f, 0.5f));
            int batchIndex = entityInQueryIndex / BatchSize;
            int itemIndex = entityInQueryIndex % BatchSize;
            Matrix4x4* batchArray = (Matrix4x4*)pins[batchIndex];
            batchArray[itemIndex] = matrix;
        }).ScheduleParallel(Dependency).Complete();

        for (int i = 0; i < batchCount; i++)
        {
            m_pins[i].Free();
        }

        pins.Dispose();

        for (int i = 0; i < batchCount; i++)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, m_matrices[i], i < batchCount - 1 ? BatchSize : lastBatchSize);
        }

        //Entities.WithAll<DroneTag>().WithStoreEntityQueryInField(ref m_query).ForEach((Entity entity, in Offset offset, in SmoothPosition smoothPosition) =>
        //{
        //    var pos = smoothPosition.Value + offset.Value;

        //    var matrix = Matrix4x4.Translate(new float3(pos.x, .5f, pos.y)) * Matrix4x4.Scale(Vector3.one * .5f);
        //    Graphics.DrawMesh(mesh, matrix, material, 0);
        //}).Run();
    }
}
