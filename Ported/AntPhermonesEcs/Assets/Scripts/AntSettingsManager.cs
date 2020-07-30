using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class AntSettingsManager : MonoBehaviour
{
    public AntSettings Settings;
    public AntSettingsData SettingsData;

    public static AntSettings Current
    {
        get { return FindObjectOfType<AntSettingsManager>().Settings; }
    }

    public static AntSettingsData CurrentData
    {
        get { return FindObjectOfType<AntSettingsManager>().SettingsData; }
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
        SettingsData.searchColor = manager.searchColor;
        SettingsData.carryColor = manager.carryColor;
        SettingsData.antCount = manager.antCount;
        SettingsData.mapSize = manager.mapSize;
        SettingsData.bucketResolution = manager.bucketResolution;
        SettingsData.antSize = manager.antSize;
        SettingsData.antSpeed = manager.antSpeed;
        SettingsData.antAccel = manager.antAccel;
        SettingsData.trailAddSpeed = manager.trailAddSpeed;
        SettingsData.trailDecay = manager.trailDecay;
        SettingsData.randomSteering = manager.randomSteering;
        SettingsData.pheromoneSteerStrength = manager.pheromoneSteerStrength;
        SettingsData.wallSteerStrength = manager.wallSteerStrength;
        SettingsData.goalSteerStrength = manager.goalSteerStrength;
        SettingsData.outwardStrength = manager.outwardStrength;
        SettingsData.inwardStrength = manager.inwardStrength;
        SettingsData.rotationResolution = manager.rotationResolution;
        SettingsData.obstacleRingCount = manager.obstacleRingCount;
        SettingsData.obstaclesPerRing = manager.obstaclesPerRing;
        SettingsData.obstacleRadius = manager.obstacleRadius;
    }
}
