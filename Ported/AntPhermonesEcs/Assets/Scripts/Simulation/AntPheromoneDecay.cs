using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
    [BurstCompile]
    struct DecayJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PheromoneBufferElement> Buffer;
        public Entity Map;
        public float Decay;

        public void Execute(int index)
        {
            Buffer[Map].ElementAt(index).Value *= Decay;
        }
    }

    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;

        RequireSingletonForUpdate<PheromoneBufferElement>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int mapSize = m_settings.mapSize;

        DecayJob job;
        job.Map = GetSingletonEntity<MapSettings>();
        job.Buffer = GetBufferFromEntity<PheromoneBufferElement>(false);
        job.Decay = m_settings.trailDecay;
        return job.Schedule(mapSize * mapSize, 128, inputDeps);
    }
}