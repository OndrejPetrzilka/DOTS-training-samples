using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct AntSettingsData
{
    public Color searchColor;
    public Color carryColor;
    public int antCount;
    public int mapSize;
    public int bucketResolution;
    public Vector3 antSize;
    public float antSpeed;
    [Range(0f, 1f)]
    public float antAccel;
    public float trailAddSpeed;
    [Range(0f, 1f)]
    public float trailDecay;
    public float randomSteering;
    public float pheromoneSteerStrength;
    public float wallSteerStrength;
    public float goalSteerStrength;
    public float outwardStrength;
    public float inwardStrength;
    public int rotationResolution;
    public int obstacleRingCount;
    [Range(0f, 1f)]
    public float obstaclesPerRing;
    public float obstacleRadius;

    public Vector2 colonyPosition
    {
        get { return Vector2.one * mapSize * .5f; }
    }
}
