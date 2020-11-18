using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;

[assembly: RegisterGenericJobType(typeof(AddGenericComponentJob<FindPath>))]
public struct FindPath : IComponentData
{
    public int ComponentTypeIndex;
    public byte Filters;
    public FindPathFlags Flags;

    public FindPath(int componentTypeIndex = -1, byte filters = 0, FindPathFlags flags = FindPathFlags.None)
    {
        ComponentTypeIndex = componentTypeIndex;
        Filters = filters;
        Flags = flags;
    }

    public static FindPath Create<T>(FindPathFlags flags = FindPathFlags.None, byte filters = 0)
        where T : struct, IComponentData
    {
        FindPath result;
        result.ComponentTypeIndex = TypeManager.GetTypeIndex<T>();
        result.Filters = filters;
        result.Flags = flags;
        return result;
    }
}
