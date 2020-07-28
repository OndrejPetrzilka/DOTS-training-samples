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
public class ResourceGenerator : ComponentSystem
{
    AntSettings m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;

        GenerateResource();
    }

    protected override void OnUpdate()
    {
    }

    void GenerateResource()
    {
        float resourceAngle = Random.value * 2f * Mathf.PI;
        float2 resourcePosition = Vector2.one * m_settings.mapSize * .5f + new Vector2(Mathf.Cos(resourceAngle) * m_settings.mapSize * .475f, Mathf.Sin(resourceAngle) * m_settings.mapSize * .475f);

        EntityManager.CreateEntity(typeof(Resource));
        SetSingleton(new Resource() { Position = resourcePosition });
    }
}
