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
public class ObstacleGenerator : ComponentSystem
{
    AntSettingsData m_settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.CurrentData;
        GenerateObstacles();
    }

    protected override void OnUpdate()
    {
    }

    void GenerateObstacles()
    {
        EntityArchetype archetype = EntityManager.CreateArchetype(typeof(Position), typeof(Radius), typeof(ObstacleTag));

        int index = 0;
        for (int i = 1; i <= m_settings.obstacleRingCount; i++)
        {
            float ringRadius = (i / (m_settings.obstacleRingCount + 1f)) * (m_settings.mapSize * .5f);
            float circumference = ringRadius * 2f * Mathf.PI;
            int maxCount = Mathf.CeilToInt(circumference / (2f * m_settings.obstacleRadius) * 2f);
            int offset = Random.Range(0, maxCount);
            int holeCount = Random.Range(1, 3);
            for (int j = 0; j < maxCount; j++)
            {
                float t = (float)j / maxCount;
                if ((t * holeCount) % 1f < m_settings.obstaclesPerRing)
                {
                    float angle = (j + offset) / (float)maxCount * (2f * Mathf.PI);
                    float2 position = new float2(m_settings.mapSize * .5f + Mathf.Cos(angle) * ringRadius, m_settings.mapSize * .5f + Mathf.Sin(angle) * ringRadius);
                    float radius = m_settings.obstacleRadius;
                    var entity = EntityManager.CreateEntity(archetype);
                    EntityManager.SetName(entity, "Obstacle" + index);
                    EntityManager.SetComponentData(entity, new Position { Value = position });
                    EntityManager.SetComponentData(entity, new Radius { Value = radius });
                    index++;
                    //Debug.DrawRay(obstacle.position / mapSize,-Vector3.forward * .05f,Color.green,10000f);
                }
            }
        }

        //obstacleMatrices = new Matrix4x4[Mathf.CeilToInt((float)output.Count / instancesPerBatch)][];
        //for (int i = 0; i < obstacleMatrices.Length; i++)
        //{
        //    obstacleMatrices[i] = new Matrix4x4[Mathf.Min(instancesPerBatch, output.Count - i * instancesPerBatch)];
        //    for (int j = 0; j < obstacleMatrices[i].Length; j++)
        //    {
        //        obstacleMatrices[i][j] = Matrix4x4.TRS(output[i * instancesPerBatch + j].position / mapSize, Quaternion.identity, new Vector3(obstacleRadius * 2f, obstacleRadius * 2f, 1f) / mapSize);
        //    }
        //}

        //obstacles = output.ToArray();

        //List<Obstacle_old>[,] tempObstacleBuckets = new List<Obstacle_old>[bucketResolution, bucketResolution];

        //for (int x = 0; x < bucketResolution; x++)
        //{
        //    for (int y = 0; y < bucketResolution; y++)
        //    {
        //        tempObstacleBuckets[x, y] = new List<Obstacle_old>();
        //    }
        //}

        //for (int i = 0; i < obstacles.Length; i++)
        //{
        //    Vector2 pos = obstacles[i].position;
        //    float radius = obstacles[i].radius;
        //    for (int x = Mathf.FloorToInt((pos.x - radius) / mapSize * bucketResolution); x <= Mathf.FloorToInt((pos.x + radius) / mapSize * bucketResolution); x++)
        //    {
        //        if (x < 0 || x >= bucketResolution)
        //        {
        //            continue;
        //        }
        //        for (int y = Mathf.FloorToInt((pos.y - radius) / mapSize * bucketResolution); y <= Mathf.FloorToInt((pos.y + radius) / mapSize * bucketResolution); y++)
        //        {
        //            if (y < 0 || y >= bucketResolution)
        //            {
        //                continue;
        //            }
        //            tempObstacleBuckets[x, y].Add(obstacles[i]);
        //        }
        //    }
        //}

        //obstacleBuckets = new Obstacle_old[bucketResolution, bucketResolution][];
        //for (int x = 0; x < bucketResolution; x++)
        //{
        //    for (int y = 0; y < bucketResolution; y++)
        //    {
        //        obstacleBuckets[x, y] = tempObstacleBuckets[x, y].ToArray();
        //    }
        //}
    }
}
