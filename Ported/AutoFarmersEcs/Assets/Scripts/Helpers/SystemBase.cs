using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public abstract class SystemBase : Unity.Entities.SystemBase
{
    public QueryBuilder Query
    {
        get { return new QueryBuilder(this); }
    }

    public new EntityQuery GetEntityQuery(params EntityQueryDesc[] desc)
    {
        return base.GetEntityQuery(desc);
    }
}
