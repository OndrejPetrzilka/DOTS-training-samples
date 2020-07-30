using System;
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
[UpdateAfter(typeof(AntSteering))]
public class AntPheromoneDrop : SystemBase
{
    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
    }

    protected override void OnUpdate()
    {
        int mapSize = m_settings.mapSize;
        float antSpeed = m_settings.antSpeed;
        float trailAddSpeed = m_settings.trailAddSpeed;
        float deltaTime = UnityEngine.Time.fixedDeltaTime;

        var map = GetSingletonEntity<MapSettings>();
        var buffer = GetBufferFromEntity<PheromoneBufferElement>();

        Entities.WithName("DropR").WithAll<AntTag, HoldingResourceTag>().ForEach((Entity e, in Position position, in Speed speed) =>
        {
            float excitement = 1f;
            excitement *= speed.Value / antSpeed;

            int x = Mathf.FloorToInt(position.Value.x);
            int y = Mathf.FloorToInt(position.Value.y);
            if (x >= 0 && y >= 0 && x < mapSize && y < mapSize)
            {
                int index = AntPheromones.PheromoneIndex(x, y, mapSize);
                ref PheromoneBufferElement value = ref buffer[map].ElementAt(index);
                value.Value += (trailAddSpeed * excitement * deltaTime) * (1f - value.Value);
                if (value.Value > 1f)
                {
                    value.Value = 1f;
                }
            }
        }).Schedule();

        Entities.WithName("DropN").WithAll<AntTag>().WithNone<HoldingResourceTag>().ForEach((Entity e, in Position position, in Speed speed) =>
        {
            float excitement = 0.3f;
            excitement *= speed.Value / antSpeed;

            int x = Mathf.FloorToInt(position.Value.x);
            int y = Mathf.FloorToInt(position.Value.y);
            if (x >= 0 && y >= 0 && x < mapSize && y < mapSize)
            {
                int index = AntPheromones.PheromoneIndex(x, y, mapSize);
                ref PheromoneBufferElement value = ref buffer[map].ElementAt(index);
                value.Value += (trailAddSpeed * excitement * deltaTime) * (1f - value.Value);
                if (value.Value > 1f)
                {
                    value.Value = 1f;
                }
            }
        }).Schedule();
    }
}