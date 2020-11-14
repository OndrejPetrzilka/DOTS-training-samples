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
    public static WorldSettings GetSettings(this ComponentSystemBase system)
    {
        var entity = system.GetSingletonEntity<WorldSettings>();
        return system.EntityManager.GetComponentData<WorldSettings>(entity);
    }

    public static RenderSettings GetRenderSettings(this ComponentSystemBase system)
    {
        return system.World.GetOrCreateSystem<RenderSettingsSystem>().Settings;
    }
}
