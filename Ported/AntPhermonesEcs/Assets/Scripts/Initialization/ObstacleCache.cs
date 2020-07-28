using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ObstacleCache : ComponentSystem
{
    public struct Obstacle
    {
        public float2 Position;
        public float Radius;
    }

    public struct BucketInfo
    {
        public int Offset;
        public int Count;
    }

    AntSettings m_settings;
    NativeArray<Obstacle> m_obstacles;
    NativeArray<BucketInfo> m_buckets;

    public NativeArray<Obstacle> Obstacles
    {
        get { return m_obstacles; }
    }

    public NativeArray<BucketInfo> Buckets
    {
        get { return m_buckets; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
    }

    protected override void OnDestroy()
    {
        if (m_obstacles.IsCreated)
        {
            m_obstacles.Dispose();
            m_buckets.Dispose();
        }
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        if (!m_obstacles.IsCreated)
        {
            var query = GetEntityQuery(typeof(ObstacleTag), typeof(Position), typeof(Radius));
            var obstaclePositions = query.ToComponentDataArray<Position>(Allocator.Temp);
            var obstacleRadius = query.ToComponentDataArray<Radius>(Allocator.Temp);
            CreateBuckets(obstaclePositions, obstacleRadius);
            obstaclePositions.Dispose();
            obstacleRadius.Dispose();
        }
    }

    void CreateBuckets(NativeArray<Position> obstaclePositions, NativeArray<Radius> obstacleRadius)
    {
        List<Obstacle>[,] tempObstacleBuckets = new List<Obstacle>[m_settings.bucketResolution, m_settings.bucketResolution];

        for (int x = 0; x < m_settings.bucketResolution; x++)
        {
            for (int y = 0; y < m_settings.bucketResolution; y++)
            {
                tempObstacleBuckets[x, y] = new List<Obstacle>();
            }
        }

        int count = 0;
        for (int i = 0; i < obstaclePositions.Length; i++)
        {
            Vector2 pos = obstaclePositions[i].Value;
            float radius = obstacleRadius[i].Value;
            for (int x = Mathf.FloorToInt((pos.x - radius) / m_settings.mapSize * m_settings.bucketResolution); x <= Mathf.FloorToInt((pos.x + radius) / m_settings.mapSize * m_settings.bucketResolution); x++)
            {
                if (x < 0 || x >= m_settings.bucketResolution)
                {
                    continue;
                }
                for (int y = Mathf.FloorToInt((pos.y - radius) / m_settings.mapSize * m_settings.bucketResolution); y <= Mathf.FloorToInt((pos.y + radius) / m_settings.mapSize * m_settings.bucketResolution); y++)
                {
                    if (y < 0 || y >= m_settings.bucketResolution)
                    {
                        continue;
                    }
                    tempObstacleBuckets[x, y].Add(new Obstacle { Position = pos, Radius = radius });
                    count++;
                }
            }
        }

        m_obstacles = new NativeArray<Obstacle>(count, Allocator.Persistent);
        m_buckets = new NativeArray<BucketInfo>(m_settings.bucketResolution * m_settings.bucketResolution, Allocator.Persistent);
        int obstacleIndex = 0;
        for (int x = 0; x < m_settings.bucketResolution; x++)
        {
            for (int y = 0; y < m_settings.bucketResolution; y++)
            {
                var cell = tempObstacleBuckets[x, y];

                BucketInfo info;
                info.Offset = obstacleIndex;
                info.Count = cell.Count;
                for (int i = 0; i < cell.Count; i++)
                {
                    m_obstacles[obstacleIndex] = cell[i];
                    obstacleIndex++;
                }
                m_buckets[x + m_settings.bucketResolution * y] = info;
            }
        }
    }
}
