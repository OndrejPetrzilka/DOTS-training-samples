using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public struct RockLookup
{
    public readonly NativeArray<Entity>.ReadOnly Array;
    public readonly int2 MapSize;
    public readonly JobHandle Dependency;

    public Entity this[int x, int y]
    {
        get { return Array[x + y * MapSize.x]; }
    }

    public Entity this[int2 pos]
    {
        get { return this[pos.x, pos.y]; }
    }

    public RockLookup(NativeArray<Entity>.ReadOnly array, int2 mapSize, JobHandle dependency)
    {
        Array = array;
        MapSize = mapSize;
        Dependency = dependency;
    }
}
