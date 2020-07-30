using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class AntPheromones : JobComponentSystem
{
    AntSettings m_settings;
    AntSettingsData m_settingsData;

    Texture2D pheromoneTexture;
    Material myPheromoneMaterial;
    NativeArray<Color32> textureData;

    public static int PheromoneIndex(int x, int y, int mapSize)
    {
        return x + y * mapSize;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_settingsData = AntSettingsManager.CurrentData;

        textureData = new NativeArray<Color32>(m_settingsData.mapSize * m_settingsData.mapSize, Allocator.Persistent);

        pheromoneTexture = new Texture2D(m_settingsData.mapSize, m_settingsData.mapSize);
        pheromoneTexture.wrapMode = TextureWrapMode.Mirror;
        myPheromoneMaterial = new Material(m_settings.basePheromoneMaterial);
        myPheromoneMaterial.mainTexture = pheromoneTexture;
        m_settings.pheromoneRenderer.sharedMaterial = myPheromoneMaterial;

        RequireSingletonForUpdate<PheromoneBufferElement>();
    }

    protected override void OnDestroy()
    {
        textureData.Dispose();
        Object.DestroyImmediate(pheromoneTexture);
        base.OnDestroy();
    }
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var map = GetSingletonEntity<MapSettings>();
        var buffer = GetBufferFromEntity<PheromoneBufferElement>(false);
        var textureData = this.textureData;

        Job.WithName("Pheromones").WithReadOnly(buffer).WithCode(() =>
        {
            var buf = buffer[map];
            for (int i = 0; i < buf.Length; i++)
            {
                textureData[i] = new Color32((byte)(buf[i].Value * 255.0f), 0, 0, 1);
            }
        }).Schedule(inputDeps).Complete();

        pheromoneTexture.SetPixelData(textureData, 0);
        pheromoneTexture.Apply();
        return default;
    }
}