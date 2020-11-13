using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public class Settings : IComponentData, IEquatable<Settings>
{
    public int2 mapSize;
    public int storeCount;
    public int rockSpawnAttempts;

    public Mesh rockMesh;
    public Material rockMaterial;
    public Material plantMaterial;
    public Mesh groundMesh;
    public Material groundMaterial;
    public Mesh storeMesh;
    public Material storeMaterial;
    public AnimationCurve soldPlantYCurve;
    public AnimationCurve soldPlantXZScaleCurve;
    public AnimationCurve soldPlantYScaleCurve;

    [Space(10)]
    public Mesh farmerMesh;
    public Material farmerMaterial;
    public int initialFarmerCount;
    public int maxFarmerCount;
    [Range(0f, 1f)]
    public float movementSmooth;

    [Space(10)]
    public Mesh droneMesh;
    public Material droneMaterial;
    public int maxDroneCount;
    [Range(0f, 1f)]
    public float moveSmooth;
    [Range(0f, 1f)]
    public float carrySmooth;

    public override bool Equals(object obj)
    {
        return Equals(obj as Settings);
    }

    public bool Equals(Settings other)
    {
        return other != null &&
               mapSize.Equals(other.mapSize) &&
               storeCount == other.storeCount &&
               rockSpawnAttempts == other.rockSpawnAttempts &&
               EqualityComparer<Mesh>.Default.Equals(rockMesh, other.rockMesh) &&
               EqualityComparer<Material>.Default.Equals(rockMaterial, other.rockMaterial) &&
               EqualityComparer<Material>.Default.Equals(plantMaterial, other.plantMaterial) &&
               EqualityComparer<Mesh>.Default.Equals(groundMesh, other.groundMesh) &&
               EqualityComparer<Material>.Default.Equals(groundMaterial, other.groundMaterial) &&
               EqualityComparer<Mesh>.Default.Equals(storeMesh, other.storeMesh) &&
               EqualityComparer<Material>.Default.Equals(storeMaterial, other.storeMaterial) &&
               EqualityComparer<AnimationCurve>.Default.Equals(soldPlantYCurve, other.soldPlantYCurve) &&
               EqualityComparer<AnimationCurve>.Default.Equals(soldPlantXZScaleCurve, other.soldPlantXZScaleCurve) &&
               EqualityComparer<AnimationCurve>.Default.Equals(soldPlantYScaleCurve, other.soldPlantYScaleCurve) &&
               EqualityComparer<Mesh>.Default.Equals(farmerMesh, other.farmerMesh) &&
               EqualityComparer<Material>.Default.Equals(farmerMaterial, other.farmerMaterial) &&
               initialFarmerCount == other.initialFarmerCount &&
               maxFarmerCount == other.maxFarmerCount &&
               movementSmooth == other.movementSmooth &&
               EqualityComparer<Mesh>.Default.Equals(droneMesh, other.droneMesh) &&
               EqualityComparer<Material>.Default.Equals(droneMaterial, other.droneMaterial) &&
               maxDroneCount == other.maxDroneCount &&
               moveSmooth == other.moveSmooth &&
               carrySmooth == other.carrySmooth;
    }

    public override int GetHashCode()
    {
        int hashCode = -358547322;
        hashCode = hashCode * -1521134295 + mapSize.GetHashCode();
        hashCode = hashCode * -1521134295 + storeCount.GetHashCode();
        hashCode = hashCode * -1521134295 + rockSpawnAttempts.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(rockMesh);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(rockMaterial);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(plantMaterial);
        hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(groundMesh);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(groundMaterial);
        hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(storeMesh);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(storeMaterial);
        hashCode = hashCode * -1521134295 + EqualityComparer<AnimationCurve>.Default.GetHashCode(soldPlantYCurve);
        hashCode = hashCode * -1521134295 + EqualityComparer<AnimationCurve>.Default.GetHashCode(soldPlantXZScaleCurve);
        hashCode = hashCode * -1521134295 + EqualityComparer<AnimationCurve>.Default.GetHashCode(soldPlantYScaleCurve);
        hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(farmerMesh);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(farmerMaterial);
        hashCode = hashCode * -1521134295 + initialFarmerCount.GetHashCode();
        hashCode = hashCode * -1521134295 + maxFarmerCount.GetHashCode();
        hashCode = hashCode * -1521134295 + movementSmooth.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Mesh>.Default.GetHashCode(droneMesh);
        hashCode = hashCode * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(droneMaterial);
        hashCode = hashCode * -1521134295 + maxDroneCount.GetHashCode();
        hashCode = hashCode * -1521134295 + moveSmooth.GetHashCode();
        hashCode = hashCode * -1521134295 + carrySmooth.GetHashCode();
        return hashCode;
    }
}
