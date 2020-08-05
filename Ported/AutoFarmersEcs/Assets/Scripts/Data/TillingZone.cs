using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

public struct TillingZone : IComponentData
{
    public int2 Position;
    public int2 Size;
}
