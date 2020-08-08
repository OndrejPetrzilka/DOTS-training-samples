using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct LookupComponent : IComponentData
{
    public int ComponentTypeIndex;

    public LookupComponent(int componentTypeIndex)
    {
        ComponentTypeIndex = componentTypeIndex;
    }

    public static LookupComponent Create<T>()
    {
        return new LookupComponent(TypeManager.GetTypeIndex<T>());
    }
}