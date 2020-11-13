using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct GenerateWorld : IComponentData
{
    [Tooltip("Random generator seed, zero to use random seed")]
    public int Seed;
}
