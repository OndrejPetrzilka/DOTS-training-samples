using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(AntSteering))]
public class AntPheromoneSteering : JobComponentSystem
{
    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;

        RequireSingletonForUpdate<PheromoneBufferElement>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float distance = 3;
        int mapSize = m_settings.mapSize;

        var map = GetSingletonEntity<MapSettings>();
        var buffer = GetBufferFromEntity<PheromoneBufferElement>(true);

        return Entities.WithName("PSteer").WithBurst(FloatMode.Fast, FloatPrecision.Low).WithReadOnly(buffer).WithAll<AntTag>().ForEach((Entity e, ref PheromoneSteering steering, in Position position, in FacingAngle facing) =>
        {
            float output = 0;

            for (int i = -1; i <= 1; i += 2)
            {
                float angle = facing.Value + i * math.PI * .25f;
                float testX = position.Value.x + math.cos(angle) * distance;
                float testY = position.Value.y + math.sin(angle) * distance;

                if (testX >= 0 && testY >= 0 && testX < mapSize && testY < mapSize)
                {
                    int index = AntPheromones.PheromoneIndex((int)testX, (int)testY, mapSize);
                    float value = buffer[map][index].Value;
                    output += value * i;
                }
            }
            steering.Value = math.sign(output);
        }).Schedule(inputDeps);
    }
}