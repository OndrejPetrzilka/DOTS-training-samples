using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct LookupData : IBufferElementData
{
    public int Data;

    public int ComponentTypeIndex
    {
        get { return (Data & 0xffffff) - 1; }
    }

    public Type ComponentType
    {
        get
        {
            int index = ComponentTypeIndex;
            return index >= 0 ? TypeManager.GetType(index) : null;
        }
    }

    public byte ObjectFilters
    {
        get { return (byte)(Data >> 24); }
    }

    public LookupData(int componentTypeIndex, byte objectFilters)
    {
        Data = (((componentTypeIndex + 1) & 0xffffff) | objectFilters << 24);
    }

    public bool Equals(int componentTypeIndex, byte objectFilters)
    {
        return new LookupData(componentTypeIndex, objectFilters).Data == Data;
    }
}
