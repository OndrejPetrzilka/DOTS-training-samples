using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntSteering : ComponentSystem
{
    AntSettingsData m_settings;
    ObstacleCache m_cache;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cache = World.GetExistingSystem<ObstacleCache>();
        m_settings = AntSettingsManager.CurrentData;
    }

    protected override void OnUpdate()
    {
        Entities.WithAll<AntTag>().ForEach((Entity e, ref Position position, ref Speed speed, ref FacingAngle facing, ref TargetPosition target, ref PheromoneSteering pheromoneSteering) =>
        {
            float targetSpeed = m_settings.antSpeed;

            facing.Value += Random.Range(-m_settings.randomSteering, m_settings.randomSteering);

            float pheroSteering = pheromoneSteering.Value;
            int wallSteering = WallSteering(position.Value, facing.Value, 1.5f);
            facing.Value += pheroSteering * m_settings.pheromoneSteerStrength;
            facing.Value += wallSteering * m_settings.wallSteerStrength;

            targetSpeed *= 1f - (Mathf.Abs(pheroSteering) + Mathf.Abs(wallSteering)) / 3f;

            speed.Value += (targetSpeed - speed.Value) * m_settings.antAccel;

            if (Linecast(position.Value, target.Value) == false)
            {
                Color color = Color.green;
                float targetAngle = Mathf.Atan2(target.Value.y - position.Value.y, target.Value.x - position.Value.x);
                if (targetAngle - facing.Value > Mathf.PI)
                {
                    facing.Value += Mathf.PI * 2f;
                    color = Color.red;
                }
                else if (targetAngle - facing.Value < -Mathf.PI)
                {
                    facing.Value -= Mathf.PI * 2f;
                    color = Color.red;
                }
                else
                {
                    if (Mathf.Abs(targetAngle - facing.Value) < Mathf.PI * .5f)
                        facing.Value += (targetAngle - facing.Value) * m_settings.goalSteerStrength;
                }

                //Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
            }

            if (math.lengthsq(position.Value - target.Value) < 4f * 4f)
            {
                if (EntityManager.HasComponent<HoldingResourceTag>(e))
                {
                    EntityManager.RemoveComponent<HoldingResourceTag>(e);
                }
                else
                {
                    EntityManager.AddComponent<HoldingResourceTag>(e);
                }
                facing.Value += math.PI;
            }

            float vx = Mathf.Cos(facing.Value) * speed.Value;
            float vy = Mathf.Sin(facing.Value) * speed.Value;
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
            var obstacles = m_cache.Obstacles;
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
                    dist = Mathf.Sqrt(sqrDist);
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
            if (EntityManager.HasComponent<HoldingResourceTag>(e))
            {
                inwardOrOutward = m_settings.inwardStrength;
                pushRadius = m_settings.mapSize;
            }
            dx = m_settings.colonyPosition.x - position.Value.x;
            dy = m_settings.colonyPosition.y - position.Value.y;
            dist = Mathf.Sqrt(dx * dx + dy * dy);
            inwardOrOutward *= 1f - Mathf.Clamp01(dist / pushRadius);
            vx += dx / dist * inwardOrOutward;
            vy += dy / dist * inwardOrOutward;

            if (ovx != vx || ovy != vy)
            {
                facing.Value = Mathf.Atan2(vy, vx);
            }

        });
    }

    ObstacleCache.BucketInfo GetObstacleBucket(Vector2 pos)
    {
        return GetObstacleBucket(pos.x, pos.y);
    }

    ObstacleCache.BucketInfo GetObstacleBucket(float posX, float posY)
    {
        int x = (int)(posX / m_settings.mapSize * m_settings.bucketResolution);
        int y = (int)(posY / m_settings.mapSize * m_settings.bucketResolution);
        if (x < 0 || y < 0 || x >= m_settings.bucketResolution || y >= m_settings.bucketResolution)
        {
            return default;
        }
        else
        {
            return m_cache.Buckets[x + m_settings.bucketResolution * y];
        }
    }

    int WallSteering(float2 position, float facingAngle, float distance)
    {
        int output = 0;

        for (int i = -1; i <= 1; i += 2)
        {
            float angle = facingAngle + i * Mathf.PI * .25f;
            float testX = position.x + Mathf.Cos(angle) * distance;
            float testY = position.y + Mathf.Sin(angle) * distance;

            if (testX < 0 || testY < 0 || testX >= m_settings.mapSize || testY >= m_settings.mapSize)
            {

            }
            else
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

    bool Linecast(Vector2 point1, Vector2 point2)
    {
        float dx = point2.x - point1.x;
        float dy = point2.y - point1.y;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        int stepCount = Mathf.CeilToInt(dist * .5f);
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
