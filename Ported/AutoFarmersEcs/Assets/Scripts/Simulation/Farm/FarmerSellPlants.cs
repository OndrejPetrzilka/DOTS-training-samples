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
    static readonly int m_storeIndex = TypeManager.GetTypeIndex<StoreTag>();
    static readonly int m_plantIndex = TypeManager.GetTypeIndex<PlantTag>();
    static readonly ComponentTypes m_removeJobTypes = new ComponentTypes(typeof(PathTarget), typeof(PathData), typeof(PathFinished), typeof(FindPath), typeof(CarryingPlant));

    EntityArchetype m_farmerArchetype;
    EntityCommandBufferSystem m_cmdSystem;

    int m_money;

    EntityQuery m_farmers;
    EntityQuery m_pathFailed;
    private EntityQuery m_findPlantQuery;
    private EntityQuery m_reachedPlantQuery;
    private EntityQuery m_findStoreQuery;
    private EntityQuery m_sellPlantQuery;

    public int Money
    {
        get { return m_money; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_farmerArchetype = EntityManager.CreateArchetype(typeof(FarmerTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        m_farmers = EntityManager.CreateEntityQuery(typeof(FarmerTag));
        m_pathFailed = Query.WithAll<FarmerTag, WorkSellPlants, PathFailed>();
        m_findPlantQuery = Query.WithAll<FarmerTag, WorkSellPlants>().WithNone<FindPath, PathTarget, PathFailed, PathFinished, CarryingPlant>();
        m_findStoreQuery = Query.WithAll<FarmerTag, WorkSellPlants, CarryingPlant>().WithNone<FindPath, PathTarget, PathFailed, PathFinished>();
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;
        var maxFarmerCount = Settings.MaxFarmerCount;

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
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var plantTag = GetComponentDataFromEntity<PlantTag>(true);
            var removeComponents = m_removeJobTypes;

            Entities.WithReadOnly(plantTag).WithAll<FarmerTag, WorkSellPlants, PathFinished>().WithNone<CarryingPlant>().WithStoreEntityQueryInField(ref m_reachedPlantQuery).ForEach((Entity e, int entityInQueryIndex, in PathTarget target, in Position position) =>
            {
                // Carry plant
                cmdBuffer.RemoveComponent(entityInQueryIndex, e, removeComponents);
                if (plantTag.HasComponent(target.Entity))
                {
                    cmdBuffer.AddComponent(entityInQueryIndex, e, new CarryingPlant { Seed = plantTag[target.Entity].Seed });
                    cmdBuffer.DestroyEntity(entityInQueryIndex, target.Entity);
                }
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Find path to store
        Dependency = m_cmdSystem.AddComponentJob(m_findStoreQuery, FindPath.Create<StoreTag>(), Dependency);

        // Reached store
        if (!m_sellPlantQuery.IsEmptyIgnoreFilter)
        {
            int startFarmerCount = m_farmers.CalculateEntityCount();
            int startMoney = m_money;
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var removeJobTypes = m_removeJobTypes;
            var farmerArchetype = m_farmerArchetype;

            m_money += m_sellPlantQuery.CalculateEntityCount();
            int farmerBuyCount = Math.Min(m_money / 10, maxFarmerCount - startFarmerCount);
            m_money -= farmerBuyCount * 10;

            Entities.WithAll<WorkSellPlants, CarryingPlant, PathFinished>().WithStoreEntityQueryInField(ref m_sellPlantQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in Position position) =>
            {
                // Sell plant
                int money = startMoney + entityInQueryIndex + 1;
                int farmerCount = startFarmerCount + money / 10;
                if (money % 10 == 0 && farmerCount <= maxFarmerCount)
                {
                    var pos = position.Value;
                    var farmer = cmdBuffer.CreateEntity(entityInQueryIndex, farmerArchetype);
                    //cmdBuffer.SetName(farmer, $"Farmer {farmerCount}");
                    cmdBuffer.SetComponent(entityInQueryIndex, farmer, new Position { Value = pos });
                    cmdBuffer.SetComponent(entityInQueryIndex, farmer, new SmoothPosition { Value = pos });
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