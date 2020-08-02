using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;

public struct StoreLookup
{
    public readonly NativeArray<byte>.ReadOnly Array;
    public readonly int2 MapSize;

    public bool this[int x, int y]
    {
        get { return Array[x + y * MapSize.x] != 0; }
    }

    public bool this[int2 pos]
    {
        get { return this[pos.x, pos.y]; }
    }

    public StoreLookup(NativeArray<byte>.ReadOnly array, int2 mapSize)
    {
        Array = array;
        MapSize = mapSize;
    }
}
