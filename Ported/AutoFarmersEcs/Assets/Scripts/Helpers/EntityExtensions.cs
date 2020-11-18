using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

public static class EntityExtensions
{
    public static DynamicBuffer<T> GetSingleton<T>(this ComponentSystemBase system)
        where T : struct, IBufferElementData
    {
        return system.EntityManager.GetBuffer<T>(system.GetSingletonEntity<T>());
    }

    public static QueryBuilder GetBuilder(this SystemBase system)
    {
        return new QueryBuilder(system);
    }

    public static void Initialize<T>(this DynamicBuffer<T> buffer, int length, T defaultValue = default)
        where T : struct
    {
        buffer.Length = length;
        for (int i = 0; i < length; i++)
        {
            buffer[i] = defaultValue;
        }
    }
}
