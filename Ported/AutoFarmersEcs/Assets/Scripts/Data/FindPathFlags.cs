using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Flags]
public enum FindPathFlags : byte
{
    None = 0,
    UseGroundState = 1,
    GroundStateTilled = 2,
    Flying = 3,
}
