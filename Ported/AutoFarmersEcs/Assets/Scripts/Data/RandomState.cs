using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public struct RandomState : IComponentData
{
    public Random Rng;

    public RandomState(uint seed)
    {
        Rng = new Random(seed);
    }
}
