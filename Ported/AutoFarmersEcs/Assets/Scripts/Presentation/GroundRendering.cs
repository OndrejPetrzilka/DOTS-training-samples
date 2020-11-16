using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class GroundRendering : SystemBase
{
    struct Batch
    {
        public Matrix4x4[] Matrices;
        public MaterialPropertyBlock Block;
    }

    const int BatchSize = 1023; // DrawMeshInstanced limitation
    int size = -1;

    RenderSettings m_settings;
    Batch[] m_batches;
    float[] m_tmpTill = new float[BatchSize];

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Ground>();
        m_settings = this.GetRenderSettings();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;
        var mesh = m_settings.groundMesh;
        var material = m_settings.groundMaterial;
        var ground = GetBuffer<Ground>(GetSingletonEntity<Ground>());

        var rng = new Random(1);
        if (ground.Length != size)
        {
            size = ground.Length;
            int batchCount = Mathf.CeilToInt(ground.Length / (float)BatchSize);
            m_batches = new Batch[batchCount];
            for (int b = 0; b < batchCount; b++)
            {
                int batchLen = Mathf.Min(BatchSize, size - b * BatchSize);

                Batch batch;
                batch.Matrices = new Matrix4x4[batchLen];
                batch.Block = new MaterialPropertyBlock();
                m_batches[b] = batch;

                for (int i = 0; i < batchLen; i++)
                {
                    int index = b * BatchSize + i;
                    int x = index % mapSize.x;
                    int y = index / mapSize.x;
                    Vector3 pos = new Vector3(x + .5f, 0f, y + .5f);
                    float zRot = rng.NextInt(0, 2) * 180f;
                    batch.Matrices[i] = Matrix4x4.TRS(pos, Quaternion.Euler(90f, 0f, zRot), Vector3.one);
                }
            }
        }

        for (int b = 0; b < m_batches.Length; b++)
        {
            int batchLen = Mathf.Min(BatchSize, size - b * BatchSize);
            for (int i = 0; i < batchLen; i++)
            {
                int index = b * BatchSize + i;
                var item = ground[index];
                m_tmpTill[i] = item.Till;
            }
            var batch = m_batches[b];
            batch.Block.SetFloatArray("_Tilled", m_tmpTill);
            Graphics.DrawMeshInstanced(mesh, 0, material, batch.Matrices, batch.Matrices.Length, batch.Block);
        }
    }
}
