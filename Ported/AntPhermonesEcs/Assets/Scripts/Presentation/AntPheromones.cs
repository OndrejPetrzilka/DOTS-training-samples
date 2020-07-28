using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public class AntPheromones : ComponentSystem
{
    AntSettings m_settings;

    Texture2D pheromoneTexture;
    Material myPheromoneMaterial;
    NativeArray<float> pheromones;
    NativeArray<Color32> textureData;
    AntPheromoneDecay m_decay;

    public NativeArray<float> Pheromones
    {
        get { return pheromones; }
    }

    public static int PheromoneIndex(int x, int y, int mapSize)
    {
        return x + y * mapSize;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;

        pheromones = new NativeArray<float>(m_settings.mapSize * m_settings.mapSize, Allocator.Persistent);
        textureData = new NativeArray<Color32>(m_settings.mapSize * m_settings.mapSize, Allocator.Persistent);

        pheromoneTexture = new Texture2D(m_settings.mapSize, m_settings.mapSize);
        pheromoneTexture.wrapMode = TextureWrapMode.Mirror;
        myPheromoneMaterial = new Material(m_settings.basePheromoneMaterial);
        myPheromoneMaterial.mainTexture = pheromoneTexture;
        m_settings.pheromoneRenderer.sharedMaterial = myPheromoneMaterial;

        m_decay = World.GetExistingSystem<AntPheromoneDecay>();
    }

    protected override void OnDestroy()
    {
        pheromones.Dispose();
        textureData.Dispose();
        Object.DestroyImmediate(pheromoneTexture);
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        m_decay.Handle.Complete();
        for (int i = 0; i < pheromones.Length; i++)
        {
            textureData[i] = new Color32((byte)(pheromones[i] * 255.0f), 0, 0, 1);
        }

        pheromoneTexture.SetPixelData(textureData, 0);
        //pheromoneTexture.SetPixels(pheromones);
        pheromoneTexture.Apply();
    }
}