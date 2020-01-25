using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class AntSettingsManager : MonoBehaviour
{
    public AntSettings Settings;

    public static AntSettings Current
    {
        get { return FindObjectOfType<AntSettingsManager>().Settings; }
    }

    [ContextMenu("Find from manager")]
    void FindFromManager()
    {
        LoadFrom(Resources.FindObjectsOfTypeAll<AntManager>()[0]);
    }

    public void LoadFrom(AntManager manager)
    {
        Settings.basePheromoneMaterial = manager.basePheromoneMaterial;
        Settings.pheromoneRenderer = manager.pheromoneRenderer;
        Settings.antMaterial = manager.antMaterial;
        Settings.obstacleMaterial = manager.obstacleMaterial;
        Settings.resourceMaterial = manager.resourceMaterial;
        Settings.colonyMaterial = manager.colonyMaterial;
        Settings.antMesh = manager.antMesh;
        Settings.obstacleMesh = manager.obstacleMesh;
        Settings.colonyMesh = manager.colonyMesh;
        Settings.resourceMesh = manager.resourceMesh;
        Settings.searchColor = manager.searchColor;
        Settings.carryColor = manager.carryColor;
        Settings.antCount = manager.antCount;
        Settings.mapSize = manager.mapSize;
        Settings.bucketResolution = manager.bucketResolution;
        Settings.antSize = manager.antSize;
        Settings.antSpeed = manager.antSpeed;
        Settings.antAccel = manager.antAccel;
        Settings.trailAddSpeed = manager.trailAddSpeed;
        Settings.trailDecay = manager.trailDecay;
        Settings.randomSteering = manager.randomSteering;
        Settings.pheromoneSteerStrength = manager.pheromoneSteerStrength;
        Settings.wallSteerStrength = manager.wallSteerStrength;
        Settings.goalSteerStrength = manager.goalSteerStrength;
        Settings.outwardStrength = manager.outwardStrength;
        Settings.inwardStrength = manager.inwardStrength;
        Settings.rotationResolution = manager.rotationResolution;
        Settings.obstacleRingCount = manager.obstacleRingCount;
        Settings.obstaclesPerRing = manager.obstaclesPerRing;
        Settings.obstacleRadius = manager.obstacleRadius;
    }
}
