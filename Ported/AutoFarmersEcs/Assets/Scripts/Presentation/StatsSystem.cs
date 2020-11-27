using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

public class StatsSystem : SystemBase
{
    public static int FarmerCount;
    public static int DroneCount;
    public static int PlantCount;
    public static int RockCount;
    public static int MoneyForFarmers;
    public static int MoneyForDrones;

    public static int Version;

    EntityQuery m_farmers;
    EntityQuery m_drones;
    EntityQuery m_plants;
    EntityQuery m_rocks;

    FarmerSellPlants m_sellSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_farmers = GetEntityQuery(typeof(FarmerTag));
        m_drones = GetEntityQuery(typeof(DroneTag));
        m_plants = GetEntityQuery(typeof(PlantTag));
        m_rocks = GetEntityQuery(typeof(RockTag));
        m_sellSystem = EntityManager.World.GetOrCreateSystem<FarmerSellPlants>();
    }

    protected override void OnUpdate()
    {
        SetValue(ref FarmerCount, m_farmers);
        SetValue(ref DroneCount, m_drones);
        SetValue(ref PlantCount, m_plants);
        SetValue(ref RockCount, m_rocks);
        SetValue(ref MoneyForFarmers, m_sellSystem.MoneyForFarmers);
        SetValue(ref MoneyForDrones, m_sellSystem.MoneyForDrones);
    }

    static void SetValue(ref int storage, int newValue)
    {
        if (storage != newValue)
        {
            storage = newValue;
            Version++;
        }
    }

    static void SetValue(ref int storage, EntityQuery query)
    {
        SetValue(ref storage, query.CalculateEntityCount());
    }
}
