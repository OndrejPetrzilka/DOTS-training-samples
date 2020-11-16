using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Object = UnityEngine.Object;

public static class SettingsExtensions
{
    public static RenderSettings GetRenderSettings(this ComponentSystemBase system)
    {
        return system.World.GetOrCreateSystem<RenderSettingsSystem>().RenderSettings;
    }
}
