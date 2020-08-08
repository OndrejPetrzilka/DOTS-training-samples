using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct LookupComponentFilters : IComponentData
{
    public byte Value;

    public LookupComponentFilters(byte value)
    {
        Value = value;
    }
}