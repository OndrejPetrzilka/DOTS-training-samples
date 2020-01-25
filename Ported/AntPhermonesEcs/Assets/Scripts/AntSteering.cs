using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class AntSteering : ComponentSystem
{
    AntSettings m_settings;

    Color[] pheromones;
    Texture2D pheromoneTexture;
    Material myPheromoneMaterial;

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
        Object.DestroyImmediate(pheromoneTexture);
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        Entities.WithAll<AntTag>().ForEach((Entity e, ref Position position, ref Speed speed, ref FacingAngle facing, ref TargetPosition target) =>
        {
            float targetSpeed = m_settings.antSpeed;

            facing.Value += Random.Range(-m_settings.randomSteering, m_settings.randomSteering);

            float pheroSteering = PheromoneSteering(position.Value, facing.Value, 3f);
            //int wallSteering = WallSteering(ant, 1.5f);
            int wallSteering = 0;
            facing.Value += pheroSteering * m_settings.pheromoneSteerStrength;
            facing.Value += wallSteering * m_settings.wallSteerStrength;

            targetSpeed *= 1f - (Mathf.Abs(pheroSteering) + Mathf.Abs(wallSteering)) / 3f;

            speed.Value += (targetSpeed - speed.Value) * m_settings.antAccel;

            //if (Linecast(ant.position, targetPos) == false)
            //{
            //    Color color = Color.green;
            //    float targetAngle = Mathf.Atan2(targetPos.y - ant.position.y, targetPos.x - ant.position.x);
            //    if (targetAngle - ant.facingAngle > Mathf.PI)
            //    {
            //        ant.facingAngle += Mathf.PI * 2f;
            //        color = Color.red;
            //    }
            //    else if (targetAngle - ant.facingAngle < -Mathf.PI)
            //    {
            //        ant.facingAngle -= Mathf.PI * 2f;
            //        color = Color.red;
            //    }
            //    else
            //    {
            //        if (Mathf.Abs(targetAngle - ant.facingAngle) < Mathf.PI * .5f)
            //            ant.facingAngle += (targetAngle - ant.facingAngle) * goalSteerStrength;
            //    }

            //    //Debug.DrawLine(ant.position/mapSize,targetPos/mapSize,color);
            //}

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

            //Obstacle_old[] nearbyObstacles = GetObstacleBucket(ant.position);
            //for (int j = 0; j < nearbyObstacles.Length; j++)
            //{
            //    Obstacle_old obstacle = nearbyObstacles[j];
            //    dx = ant.position.x - obstacle.position.x;
            //    dy = ant.position.y - obstacle.position.y;
            //    float sqrDist = dx * dx + dy * dy;
            //    if (sqrDist < obstacleRadius * obstacleRadius)
            //    {
            //        dist = Mathf.Sqrt(sqrDist);
            //        dx /= dist;
            //        dy /= dist;
            //        ant.position.x = obstacle.position.x + dx * obstacleRadius;
            //        ant.position.y = obstacle.position.y + dy * obstacleRadius;

            //        vx -= dx * (dx * vx + dy * vy) * 1.5f;
            //        vy -= dy * (dx * vx + dy * vy) * 1.5f;
            //    }
            //}

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
}
