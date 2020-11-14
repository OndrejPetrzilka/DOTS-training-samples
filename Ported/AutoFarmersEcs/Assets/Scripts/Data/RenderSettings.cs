using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RenderSettings : MonoBehaviour
{
    public Mesh rockMesh;
    public Material rockMaterial;
    public Material plantMaterial;
    public Mesh groundMesh;
    public Material groundMaterial;
    public Mesh storeMesh;
    public Material storeMaterial;
    public Mesh farmerMesh;
    public Material farmerMaterial;
    public Mesh droneMesh;
    public Material droneMaterial;

    public AnimationCurve soldPlantYCurve;
    public AnimationCurve soldPlantXZScaleCurve;
    public AnimationCurve soldPlantYScaleCurve;
}
