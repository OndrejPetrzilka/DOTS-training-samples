using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public struct Ground : IBufferElementData
{
    public float Till;
    public GroundState State;
}
