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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(AntPheromoneDrop))]
public class AntPheromoneDecay : JobComponentSystem
{
    AntSettings m_settings;
    AntPheromones m_pheromones;
    AntPheromoneDrop m_drop;
    JobHandle m_handle;

    public JobHandle Handle
    {
        get { return m_handle; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_pheromones = World.GetExistingSystem<AntPheromones>();
        m_drop = World.GetExistingSystem<AntPheromoneDrop>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var pheromones = m_pheromones.Pheromones;
        int mapSize = m_settings.mapSize;
        float decay = m_settings.trailDecay;

        m_handle = Job.WithName("Decay").WithCode(() =>
        {
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    int index = AntPheromones.PheromoneIndex(x, y, mapSize);
                    var value = pheromones[index];
                    value *= decay;
                    pheromones[index] = value;
                }
            }
        }).Schedule(JobHandle.CombineDependencies(m_drop.Handle, inputDeps));
        m_handle.Complete();
        return m_handle;
    }
}