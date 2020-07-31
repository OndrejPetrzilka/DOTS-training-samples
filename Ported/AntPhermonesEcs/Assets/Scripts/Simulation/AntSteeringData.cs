using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static ObstacleCache;
using Random = Unity.Mathematics.Random;

public struct AntSteeringData
{
    [ReadOnly]
    public NativeArray<Obstacle> Obstacles;

    [ReadOnly]
    public NativeArray<BucketInfo> Buckets;
    public int FrameIndex;
    public EntityCommandBuffer.ParallelWriter CommandBuffer;

    [ReadOnly]
    public ComponentDataFromEntity<HoldingResourceTag> HoldingResource;

    [NativeDisableContainerSafetyRestriction]
    public NativeArray<Random> Randoms;

    public AntSettingsData m_settings;
    public Vector2 ResourcePosition;

    public void UpdateEntity(Entity e, int entityInQueryIndex, int nativeThreadIndex, ref Position position, ref Speed speed, ref FacingAngle facing, ref TargetPosition target, in PheromoneSteering pheromoneSteering)
    {
        float targetSpeed = m_settings.antSpeed;

        var rng = Randoms[nativeThreadIndex];
        facing.Value += rng.NextFloat(-m_settings.randomSteering, m_settings.randomSteering);
        Randoms[nativeThreadIndex] = rng;

        float pheroSteering = pheromoneSteering.Value;
        int wallSteering = WallSteering(position.Value, facing.Value, 1.5f);
        facing.Value += pheroSteering * m_settings.pheromoneSteerStrength;
        facing.Value += wallSteering * m_settings.wallSteerStrength;

        targetSpeed *= 1f - (math.abs(pheroSteering) + math.abs(wallSteering)) / 3f;

        speed.Value += (targetSpeed - speed.Value) * m_settings.antAccel;

        if (Linecast(position.Value, target.Value) == false)
        {
            Color color = Color.green;
            float targetAngle = math.atan2(target.Value.y - position.Value.y, target.Value.x - position.Value.x);
            if (targetAngle - facing.Value > math.PI)
            {
                facing.Value += math.PI * 2f;
                color = Color.red;
            }
            else if (targetAngle - facing.Value < -math.PI)
            {
                facing.Value -= math.PI * 2f;
                color = Color.red;
            }
            else
            {
                if (math.abs(targetAngle - facing.Value) < math.PI * .5f)
                    facing.Value += (targetAngle - facing.Value) * m_settings.goalSteerStrength;
            }

            //Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
        }

        bool hasResource = HoldingResource.HasComponent(e);

        if (math.lengthsq(position.Value - target.Value) < 4f * 4f)
        {
            if (hasResource)
            {
                CommandBuffer.RemoveComponent<HoldingResourceTag>(entityInQueryIndex, e);
                hasResource = false;
                target.Value = ResourcePosition;
            }
            else
            {
                CommandBuffer.AddComponent<HoldingResourceTag>(entityInQueryIndex, e);
                hasResource = true;
                target.Value = m_settings.colonyPosition;
            }
            facing.Value += math.PI;
        }

        float vx = math.cos(facing.Value) * speed.Value;
        float vy = math.sin(facing.Value) * speed.Value;
        float ovx = vx;
        float ovy = vy;

        if (position.Value.x + vx < 0f || position.Value.x + vx > m_settings.mapSize)
        {
            vx = -vx;
        }
        else
        {
            position.Value.x += vx;
        }
        if (position.Value.y + vy < 0f || position.Value.y + vy > m_settings.mapSize)
        {
            vy = -vy;
        }
        else
        {
            position.Value.y += vy;
        }

        float dx, dy, dist;

        var nearbyObstacles = GetObstacleBucket(position.Value);
        var obstacles = Obstacles;
        for (int j = 0; j < nearbyObstacles.Count; j++)
        {
            var obstacle = obstacles[nearbyObstacles.Offset + j];
            var obsPos = obstacle.Position;
            var obsRad = obstacle.Radius;
            dx = position.Value.x - obsPos.x;
            dy = position.Value.y - obsPos.y;
            float sqrDist = dx * dx + dy * dy;
            if (sqrDist < obsRad * obsRad)
            {
                dist = math.sqrt(sqrDist);
                dx /= dist;
                dy /= dist;
                position.Value.x = obsPos.x + dx * obsRad;
                position.Value.y = obsPos.y + dy * obsRad;

                vx -= dx * (dx * vx + dy * vy) * 1.5f;
                vy -= dy * (dx * vx + dy * vy) * 1.5f;
            }
        }

        float inwardOrOutward = -m_settings.outwardStrength;
        float pushRadius = m_settings.mapSize * .4f;
        if (hasResource)
        {
            inwardOrOutward = m_settings.inwardStrength;
            pushRadius = m_settings.mapSize;
        }
        dx = m_settings.colonyPosition.x - position.Value.x;
        dy = m_settings.colonyPosition.y - position.Value.y;
        dist = math.sqrt(dx * dx + dy * dy);
        inwardOrOutward *= 1f - math.saturate(dist / pushRadius);
        vx += dx / dist * inwardOrOutward;
        vy += dy / dist * inwardOrOutward;

        if (ovx != vx || ovy != vy)
        {
            facing.Value = math.atan2(vy, vx);
        }
    }

    BucketInfo GetObstacleBucket(float2 pos)
    {
        return GetObstacleBucket(pos.x, pos.y);
    }

    BucketInfo GetObstacleBucket(float posX, float posY)
    {
        int x = (int)(posX / m_settings.mapSize * m_settings.bucketResolution);
        int y = (int)(posY / m_settings.mapSize * m_settings.bucketResolution);
        if (x >= 0 && y >= 0 && x < m_settings.bucketResolution && y < m_settings.bucketResolution)
        {
            return Buckets[x + m_settings.bucketResolution * y];
        }
        return default;
    }

    int WallSteering(float2 position, float facingAngle, float distance)
    {
        int output = 0;

        for (int i = -1; i <= 1; i += 2)
        {
            float angle = facingAngle + i * math.PI * .25f;
            float testX = position.x + math.cos(angle) * distance;
            float testY = position.y + math.sin(angle) * distance;

            if (testX >= 0 && testY >= 0 && testX < m_settings.mapSize && testY < m_settings.mapSize)
            {
                int value = GetObstacleBucket(testX, testY).Count;
                if (value > 0)
                {
                    output -= i;
                }
            }
        }
        return output;
    }

    bool Linecast(float2 point1, float2 point2)
    {
        float dx = point2.x - point1.x;
        float dy = point2.y - point1.y;
        float dist = math.sqrt(dx * dx + dy * dy);

        int stepCount = (int)math.ceil(dist * .5f);
        for (int i = 0; i < stepCount; i++)
        {
            float t = (float)i / stepCount;
            if (GetObstacleBucket(point1.x + dx * t, point1.y + dy * t).Count > 0)
            {
                return true;
            }
        }

        return false;
    }
}
