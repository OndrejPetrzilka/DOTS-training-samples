﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

public struct FindPath : IComponentData
{
    public int ComponentTypeIndex;
    public byte Filters;
    public FindPathFlags Flags;
}
