using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public class StatsSystem : SystemBase
{
    public static int FarmerCount;
    public static int PlantCount;
    public static int RockCount;

    public static int Version;

    EntityQuery m_farmers;
    EntityQuery m_plants;
    EntityQuery m_rocks;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_farmers = GetEntityQuery(typeof(FarmerTag));
        m_plants = GetEntityQuery(typeof(PlantTag));
        m_rocks = GetEntityQuery(typeof(RockTag));
    }

    protected override void OnUpdate()
    {
        SetValue(ref FarmerCount, m_farmers);
        SetValue(ref PlantCount, m_plants);
        SetValue(ref RockCount, m_rocks);
    }

    static void SetValue(ref int storage, EntityQuery query)
    {
        int count = query.CalculateEntityCount();
        if (storage != count)
        {
            storage = count;
            Version++;
        }
    }
}
