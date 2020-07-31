using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntSteering : JobComponentSystem
{
    AntSettingsData m_settings;
    ObstacleCache m_cache;
    EndSimulationEntityCommandBufferSystem m_EndSimulation;
    int m_index;
    NativeArray<Unity.Mathematics.Random> m_rngs;
    Vector2 m_resourcePosition;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cache = World.GetExistingSystem<ObstacleCache>();
        m_EndSimulation = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
        m_settings = AntSettingsManager.CurrentData;

        m_rngs = new NativeArray<Unity.Mathematics.Random>(JobsUtility.MaxJobThreadCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++)
        {
            m_rngs[i] = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        m_resourcePosition = GetSingleton<Resource>().Position;
    }

    protected override void OnDestroy()
    {
        m_rngs.Dispose();
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        AntSteeringData data;
        data.m_settings = m_settings;
        data.Buckets = m_cache.Buckets;
        data.CommandBuffer = m_EndSimulation.CreateCommandBuffer().AsParallelWriter();
        data.FrameIndex = Random.Range(int.MinValue, int.MaxValue);
        data.HoldingResource = GetComponentDataFromEntity<HoldingResourceTag>(true);
        data.Obstacles = m_cache.Obstacles;
        data.Randoms = m_rngs;
        data.ResourcePosition = m_resourcePosition;

        var handle = Entities.WithBurst(FloatMode.Fast, FloatPrecision.Low).WithAll<AntTag>().ForEach((Entity e, int entityInQueryIndex, int nativeThreadIndex, ref Position position, ref Speed speed, ref FacingAngle facing, ref TargetPosition target, in PheromoneSteering pheromoneSteering) =>
         {
             data.UpdateEntity(e, entityInQueryIndex, nativeThreadIndex, ref position, ref speed, ref facing, ref target, pheromoneSteering);
         }).Schedule(inputDeps);

        m_EndSimulation.AddJobHandleForProducer(handle);

        return handle;
    }
}
