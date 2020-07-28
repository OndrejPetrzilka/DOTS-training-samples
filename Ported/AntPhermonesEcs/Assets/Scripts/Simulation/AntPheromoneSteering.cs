using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    AntSettings m_settings;
    AntPheromones m_pheromones;
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
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float distance = 3;
        int mapSize = m_settings.mapSize;
        var pheromones = m_pheromones.Pheromones;

        m_handle = Entities.WithName("PSteer").WithReadOnly(pheromones).WithAll<AntTag>().ForEach((Entity e, ref Position position, ref FacingAngle facing, ref PheromoneSteering steering) =>
        {
            float output = 0;

            for (int i = -1; i <= 1; i += 2)
            {
                float angle = facing.Value + i * math.PI * .25f;
                float testX = position.Value.x + math.cos(angle) * distance;
                float testY = position.Value.y + math.sin(angle) * distance;

                if (testX < 0 || testY < 0 || testX >= mapSize || testY >= mapSize)
                {

                }
                else
                {
                    int index = AntPheromones.PheromoneIndex((int)testX, (int)testY, mapSize);
                    float value = pheromones[index];
                    output += value * i;
                }
            }
            steering.Value = math.sign(output);
        }).Schedule(inputDeps);

        return m_handle;
    }
}