using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FarmGroup))]
public class FarmerSellPlants : SystemBase
{
    const int FarmerCost = 10;
    const int DroneBatchCost = 50;
    const int DroneBatchSize = 5;

    static readonly int m_storeIndex = TypeManager.GetTypeIndex<StoreTag>();
    static readonly int m_plantIndex = TypeManager.GetTypeIndex<PlantTag>();
    static readonly ComponentTypes m_removeJobTypes = new ComponentTypes(typeof(PathTarget), typeof(PathData), typeof(PathFinished), typeof(FindPath), typeof(CarryingPlant));

    EntityArchetype m_farmerArchetype;
    EntityArchetype m_droneArchetype;
    EntityCommandBufferSystem m_cmdSystem;

    int m_moneyForFarmers;
    int m_moneyForDrones;

    EntityQuery m_farmers;
    EntityQuery m_drones;
    EntityQuery m_pathFailed;
    private EntityQuery m_findPlantQuery;
    private EntityQuery m_reachedPlantQuery;
    private EntityQuery m_findStoreQuery;
    private EntityQuery m_sellPlantQuery;

    public int MoneyForFarmers
    {
        get { return m_moneyForFarmers; }
        set { m_moneyForFarmers = value; }
    }

    public int MoneyForDrones
    {
        get { return m_moneyForDrones; }
        set { m_moneyForDrones = value; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_farmerArchetype = EntityManager.CreateArchetype(typeof(FarmerTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));
        m_droneArchetype = EntityManager.CreateArchetype(typeof(DroneTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_farmers = EntityManager.CreateEntityQuery(typeof(FarmerTag));
        m_drones = EntityManager.CreateEntityQuery(typeof(DroneTag));
        m_pathFailed = Query.WithAll<WorkSellPlants, PathFailed>();
        m_findPlantQuery = Query.WithAll<WorkSellPlants>().WithNone<FindPath, PathTarget, PathFailed, PathFinished, CarryingPlant>();
        m_findStoreQuery = Query.WithAll<WorkSellPlants, CarryingPlant>().WithNone<FindPath, PathTarget, PathFailed, PathFinished>();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;
        var maxFarmerCount = Settings.MaxFarmerCount;
        var maxDroneBatchCount = Settings.MaxDroneCount / DroneBatchSize;

        var groundEntity = GetSingletonEntity<Ground>();
        var groundData = GetBufferFromEntity<Ground>(true);

        var storeIndex = m_storeIndex;
        var plantIndex = m_plantIndex;

        // Remove work when path finding failed
        if (!m_pathFailed.IsEmptyIgnoreFilter)
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent<WorkSellPlants>(m_pathFailed);
        }

        // Find path to grown plant
        Dependency = m_cmdSystem.AddComponentJob(m_findPlantQuery, FindPath.Create<PlantTag>(FindPathFlags.None, 1), Dependency);

        // Reached grown plant
        if (!m_reachedPlantQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer();
            var plantTag = GetComponentDataFromEntity<PlantTag>(true);
            var removeComponents = m_removeJobTypes;

            // Single threaded, we don't want two farmers to collect same plant
            Entities.WithReadOnly(plantTag).WithAll<WorkSellPlants, PathFinished>().WithNone<CarryingPlant>().WithStoreEntityQueryInField(ref m_reachedPlantQuery).ForEach((Entity e, in PathTarget target, in Position position) =>
            {
                // Carry plant
                // TODO: Handle case when two farmers want to collect same plant in same frame
                cmdBuffer.RemoveComponent(e, removeComponents);
                if (plantTag.HasComponent(target.Entity))
                {
                    cmdBuffer.AddComponent(e, new CarryingPlant { Seed = plantTag[target.Entity].Seed });
                    cmdBuffer.DestroyEntity(target.Entity);
                }
            }).Schedule();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Find path to store
        Dependency = m_cmdSystem.AddComponentJob(m_findStoreQuery, FindPath.Create<StoreTag>(), Dependency);

        // Reached store
        if (!m_sellPlantQuery.IsEmptyIgnoreFilter)
        {
            int startFarmerCount = m_farmers.CalculateEntityCount();
            int startDroneBatchCount = m_drones.CalculateEntityCount() / DroneBatchSize;
            int startFarmerMoney = m_moneyForFarmers;
            int startDroneMoney = m_moneyForDrones;
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var removeJobTypes = m_removeJobTypes;
            var farmerArchetype = m_farmerArchetype;
            var droneArchetype = m_droneArchetype;

            int earnMoney = m_sellPlantQuery.CalculateEntityCount();
            m_moneyForFarmers += earnMoney;
            m_moneyForDrones += earnMoney;

            int farmerBuyCount = Math.Min(m_moneyForFarmers / FarmerCost, maxFarmerCount - startFarmerCount);
            m_moneyForFarmers -= farmerBuyCount * FarmerCost;

            int droneBatchBuyCount = Math.Min(m_moneyForDrones / DroneBatchCost, maxDroneBatchCount - startDroneBatchCount);
            m_moneyForDrones -= droneBatchBuyCount * DroneBatchCost;

            Entities.WithAll<WorkSellPlants, CarryingPlant, PathFinished>().WithStoreEntityQueryInField(ref m_sellPlantQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in Position position) =>
            {
                // Sell plant
                int farmerMoney = startFarmerMoney + entityInQueryIndex + 1;
                int farmerCount = startFarmerCount + farmerMoney / FarmerCost;
                if (farmerMoney % FarmerCost == 0 && farmerCount <= maxFarmerCount)
                {
                    var pos = position.Value;
                    var farmer = cmdBuffer.CreateEntity(entityInQueryIndex, farmerArchetype);
                    //cmdBuffer.SetName(farmer, $"Farmer {farmerCount}");
                    cmdBuffer.SetComponent(entityInQueryIndex, farmer, new Position { Value = pos });
                    cmdBuffer.SetComponent(entityInQueryIndex, farmer, new SmoothPosition { Value = pos });
                }

                int droneMoney = startDroneMoney + entityInQueryIndex + 1;
                int droneBatchCount = startDroneBatchCount + droneMoney / DroneBatchCost;
                if (droneMoney % DroneBatchCost == 0 && droneBatchCount <= maxDroneBatchCount)
                {
                    for (int i = 0; i < DroneBatchSize; i++)
                    {
                        var pos = position.Value;
                        var drone = cmdBuffer.CreateEntity(entityInQueryIndex, droneArchetype);
                        //cmdBuffer.SetName(farmer, $"Farmer {farmerCount}");
                        cmdBuffer.SetComponent(entityInQueryIndex, drone, new Position { Value = pos });
                        cmdBuffer.SetComponent(entityInQueryIndex, drone, new SmoothPosition { Value = pos });
                    }
                }

                // Remove target
                cmdBuffer.RemoveComponent(entityInQueryIndex, e, removeJobTypes);

                // Choose other work
                if (rng.Rng.NextFloat() < 0.1f)
                {
                    cmdBuffer.RemoveComponent<WorkSellPlants>(entityInQueryIndex, e);
                }
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }
    }
}