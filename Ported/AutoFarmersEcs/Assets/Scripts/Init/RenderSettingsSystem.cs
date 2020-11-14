using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class RenderSettingsSystem : SystemBase
{
    public RenderSettings Settings;

    protected override void OnCreate()
    {
        base.OnCreate();
        Settings = Object.FindObjectOfType<RenderSettings>();
    }

    protected override void OnUpdate()
    {
    }
}