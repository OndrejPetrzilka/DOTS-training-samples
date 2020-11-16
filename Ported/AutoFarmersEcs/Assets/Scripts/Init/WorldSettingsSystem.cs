using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

/// <summary>
/// Caches world settings.
/// </summary>
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(EndInitializationEntityCommandBufferSystem))]
public class WorldSettingsSystem : Unity.Entities.SystemBase
{
    public WorldSettings Settings;

    EntityQuery m_query;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_query = GetEntityQuery(typeof(WorldSettings));
    }

    protected override void OnUpdate()
    {
        var entity = m_query.GetSingletonEntity();
        Settings = EntityManager.GetComponentData<WorldSettings>(entity);
    }
}
