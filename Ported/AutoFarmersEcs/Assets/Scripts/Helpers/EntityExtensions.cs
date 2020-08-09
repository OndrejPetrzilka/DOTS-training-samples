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

    public static void AddComponentData<T>(this ComponentSystemBase system, EntityQuery entityQuery, T data)
        where T : struct, IComponentData
    {
        if (!entityQuery.IsEmptyIgnoreFilter)
        {
            using (NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.TempJob))
            {
                system.EntityManager.AddComponent(entityQuery, ComponentType.ReadWrite<T>());
                ComponentDataFromEntity<T> componentDataFromEntity = system.GetComponentDataFromEntity<T>();
                for (int i = 0; i != nativeArray.Length; i++)
                {
                    componentDataFromEntity[nativeArray[i]] = data;
                }
            }
        }
    }

    public static QueryBuilder GetBuilder(this SystemBase system)
    {
        return new QueryBuilder(system);
    }
}
