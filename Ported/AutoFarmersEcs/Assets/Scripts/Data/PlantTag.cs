﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct PlantTag : IComponentData
{
    public int Seed;
    public float Growth;
}
