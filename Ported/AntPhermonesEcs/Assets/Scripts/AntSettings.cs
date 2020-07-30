using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct AntSettings
{
    public Material basePheromoneMaterial;
    public Renderer pheromoneRenderer;
    public Material antMaterial;
    public Material obstacleMaterial;
    public Material resourceMaterial;
    public Material colonyMaterial;
    public Mesh antMesh;
    public Mesh obstacleMesh;
    public Mesh colonyMesh;
    public Mesh resourceMesh;
}
