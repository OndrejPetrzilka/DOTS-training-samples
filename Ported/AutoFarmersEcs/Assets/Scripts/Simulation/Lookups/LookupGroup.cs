using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(BeginFixedStepSimulationEntityCommandBufferSystem))]
public class LookupGroup : ComponentSystemGroup
{
}