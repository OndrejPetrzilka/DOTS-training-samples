using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(EndFixedStepSimulationEntityCommandBufferSystem))]
public class LookupGroup : ComponentSystemGroup
{
}