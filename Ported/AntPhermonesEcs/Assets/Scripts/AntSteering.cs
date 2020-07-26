using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class AntSteering : ComponentSystem
{
    struct Obstacle
    {
        public float2 Position;
        public float Radius;
    }

    AntSettings m_settings;

    Color[] pheromones;
    Texture2D pheromoneTexture;
    Material myPheromoneMaterial;
    Obstacle[,][] obstacleBuckets;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;

        pheromones = new Color[m_settings.mapSize * m_settings.mapSize];

        pheromoneTexture = new Texture2D(m_settings.mapSize, m_settings.mapSize);
        pheromoneTexture.wrapMode = TextureWrapMode.Mirror;
        myPheromoneMaterial = new Material(m_settings.basePheromoneMaterial);
        myPheromoneMaterial.mainTexture = pheromoneTexture;
        m_settings.pheromoneRenderer.sharedMaterial = myPheromoneMaterial;
    }

    protected override void OnDestroy()
    {
        obstacleBuckets = null;
        Object.DestroyImmediate(pheromoneTexture);
        base.OnDestroy();
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
                }
            }
        }

        obstacleBuckets = new Obstacle[m_settings.bucketResolution, m_settings.bucketResolution][];
        for (int x = 0; x < m_settings.bucketResolution; x++)
        {
            for (int y = 0; y < m_settings.bucketResolution; y++)
            {
                obstacleBuckets[x, y] = tempObstacleBuckets[x, y].ToArray();
            }
        }
    }

    protected override void OnUpdate()
    {
        var query = GetEntityQuery(typeof(ObstacleTag), typeof(Position), typeof(Radius));

        if (obstacleBuckets == null)
        {
            var obstaclePositions = query.ToComponentDataArray<Position>(Allocator.Temp);
            var obstacleRadius = query.ToComponentDataArray<Radius>(Allocator.Temp);
            CreateBuckets(obstaclePositions, obstacleRadius);
            obstaclePositions.Dispose();
            obstacleRadius.Dispose();
        }

        Entities.WithAll<AntTag>().ForEach((Entity e, ref Position position, ref Speed speed, ref FacingAngle facing, ref TargetPosition target) =>
        {
            float targetSpeed = m_settings.antSpeed;

            facing.Value += Random.Range(-m_settings.randomSteering, m_settings.randomSteering);

            float pheroSteering = PheromoneSteering(position.Value, facing.Value, 3f);
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

            Obstacle[] nearbyObstacles = GetObstacleBucket(position.Value);
            for (int j = 0; j < nearbyObstacles.Length; j++)
            {
                var obstacle = nearbyObstacles[j];
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

            float excitement = .3f;
            if (EntityManager.HasComponent<HoldingResourceTag>(e))
            {
                excitement = 1f;
            }
            excitement *= speed.Value / m_settings.antSpeed;
            DropPheromones(position.Value, excitement);
        });

        for (int x = 0; x < m_settings.mapSize; x++)
        {
            for (int y = 0; y < m_settings.mapSize; y++)
            {
                int index = PheromoneIndex(x, y);
                pheromones[index].r *= m_settings.trailDecay;
            }
        }

        pheromoneTexture.SetPixels(pheromones);
        pheromoneTexture.Apply();
    }

    int PheromoneIndex(int x, int y)
    {
        return x + y * m_settings.mapSize;
    }

    void DropPheromones(Vector2 position, float strength)
    {
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        if (x < 0 || y < 0 || x >= m_settings.mapSize || y >= m_settings.mapSize)
        {
            return;
        }

        int index = PheromoneIndex(x, y);
        pheromones[index].r += (m_settings.trailAddSpeed * strength * Time.fixedDeltaTime) * (1f - pheromones[index].r);
        if (pheromones[index].r > 1f)
        {
            pheromones[index].r = 1f;
        }
    }

    float PheromoneSteering(float2 position, float facingAngle, float distance)
    {
        float output = 0;

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
                int index = PheromoneIndex((int)testX, (int)testY);
                float value = pheromones[index].r;
                output += value * i;
            }
        }
        return Mathf.Sign(output);
    }

    Obstacle[] GetObstacleBucket(Vector2 pos)
    {
        return GetObstacleBucket(pos.x, pos.y);
    }

    Obstacle[] GetObstacleBucket(float posX, float posY)
    {
        int x = (int)(posX / m_settings.mapSize * m_settings.bucketResolution);
        int y = (int)(posY / m_settings.mapSize * m_settings.bucketResolution);
        if (x < 0 || y < 0 || x >= m_settings.bucketResolution || y >= m_settings.bucketResolution)
        {
            return Array.Empty<Obstacle>();
        }
        else
        {
            return obstacleBuckets[x, y];
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
                int value = GetObstacleBucket(testX, testY).Length;
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
            if (GetObstacleBucket(point1.x + dx * t, point1.y + dy * t).Length > 0)
            {
                return true;
            }
        }

        return false;
    }
}
