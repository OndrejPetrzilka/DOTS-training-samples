using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class AntGenerator : ComponentSystem
{
    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
        GenerateAnts();
    }

    protected override void OnUpdate()
    {
    }

    private void GenerateAnts()
    {
        EntityArchetype archetype = EntityManager.CreateArchetype(typeof(Position), typeof(Brightness), typeof(ColorData), typeof(FacingAngle), typeof(Speed), typeof(AntTag), typeof(PheromoneSteering));

        for (int i = 0; i < m_settings.antCount; i++)
        {
            float2 position = new float2(Random.Range(-5f, 5f) + m_settings.mapSize * 0.5f, Random.Range(-5f, 5f) + m_settings.mapSize * 0.5f);

            var entity = EntityManager.CreateEntity(archetype);
            EntityManager.SetName(entity, "Ant" + i);
            EntityManager.SetComponentData(entity, new Position { Value = position });
            EntityManager.SetComponentData(entity, new Brightness { Value = Random.Range(.75f, 1.25f) });
            EntityManager.SetComponentData(entity, new FacingAngle { Value = Random.value * Mathf.PI * 2f });
            EntityManager.SetComponentData(entity, new Speed { Value = 0 });
        }

        var map = EntityManager.CreateEntity(typeof(MapSettings));
        EntityManager.SetName(map, "Map");
        EntityManager.SetComponentData(map, new MapSettings { MapSize = m_settings.mapSize });
        var buffer = EntityManager.AddBuffer<PheromoneBufferElement>(map);
        buffer.Capacity = m_settings.mapSize * m_settings.mapSize;
        for (int i = 0; i < buffer.Capacity; i++)
        {
            buffer.Add(default);
        }
    }
}
