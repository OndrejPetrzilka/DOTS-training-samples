using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public abstract class SystemBase : Unity.Entities.SystemBase
{
    WorldSettingsSystem m_settings;

    public QueryBuilder Query
    {
        get { return new QueryBuilder(this); }
    }

    public WorldSettings Settings
    {
        get { return m_settings.Settings; }
    }

    public new EntityQuery GetEntityQuery(params EntityQueryDesc[] desc)
    {
        return base.GetEntityQuery(desc);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = World.GetOrCreateSystem<WorldSettingsSystem>();
    }
}
