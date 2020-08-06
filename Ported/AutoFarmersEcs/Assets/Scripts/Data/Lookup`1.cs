using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct Lookup<TTag> : IBufferElementData
    where TTag : struct, IComponentData
{
    public Entity Entity;

    public static implicit operator Lookup<TTag>(Entity e)
    {
        return new Lookup<TTag> { Entity = e };
    }
}
