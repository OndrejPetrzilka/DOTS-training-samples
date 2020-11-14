using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct WorldSettings : IComponentData
{
    public int2 MapSize;
    public int StoreCount;

    public int InitialFarmerCount;
    public int MaxFarmerCount;

    public int MaxDroneCount;

    [Range(0f, 1f)]
    public float MovementSmooth;

    [Range(0f, 1f)]
    public float MoveSmooth;

    [Range(0f, 1f)]
    public float CarrySmooth;

    [Tooltip("World seed")]
    public uint Seed;
}
