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
    public static Settings GetSettings(this ComponentSystemBase system)
    {
        var entity = system.GetSingletonEntity<Settings>();
        return system.EntityManager.GetComponentData<Settings>(entity);
    }
}
