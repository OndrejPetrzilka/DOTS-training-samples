using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public static class EntityExtensions
{
    public static DynamicBuffer<T> GetSingleton<T>(this ComponentSystemBase system)
        where T : struct, IBufferElementData
    {
        return system.EntityManager.GetBuffer<T>(system.GetSingletonEntity<T>());
    }
}
