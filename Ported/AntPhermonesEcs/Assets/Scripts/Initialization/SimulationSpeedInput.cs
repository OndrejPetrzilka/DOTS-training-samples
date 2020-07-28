using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class SimulationSpeedInput : ComponentSystem
{
    protected override void OnUpdate()
    {
        for (int i = 1; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                UnityEngine.Time.timeScale = i;
                //UnityEngine.Time.fixedDeltaTime = i * 1 / 50.0f;
            }
        }
    }
}
