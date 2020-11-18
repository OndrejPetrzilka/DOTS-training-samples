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
    static readonly ComponentTypes m_removeJobTypes = new ComponentTypes(typeof(WorkTarget), typeof(CarryingPlant));

    EntityArchetype m_farmerArchetype;
    EntityCommandBufferSystem m_cmdSystem;

    int m_money;

    EntityQuery m_farmers;
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
    }

    protected override void OnUpdate()
    {
        var mapSize = Settings.MapSize;
        var maxFarmerCount = Settings.MaxFarmerCount;

        var groundEntity = GetSingletonEntity<Ground>();
        var groundData = GetBufferFromEntity<Ground>(true);

        var storeIndex = m_storeIndex;
        var plantIndex = m_plantIndex;

        // TODO: Use some better system for finding target (either path finding or some simple external method)

        // Does not carry plant, find plant
        if (!m_findPlantQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

            var lookup = GetSingletonEntity<LookupData>();
            var lookupDataArray = GetBufferFromEntity<LookupData>(true);
            var lookupEntityArray = GetBufferFromEntity<LookupEntity>(true);
            var plantTag = GetComponentDataFromEntity<PlantTag>(true);

            Entities.WithReadOnly(lookupDataArray).WithReadOnly(lookupEntityArray).WithReadOnly(plantTag).WithAll<FarmerTag, WorkSellPlants>().WithNone<CarryingPlant, WorkTarget>().WithStoreEntityQueryInField(ref m_findPlantQuery).ForEach((Entity e, int entityInQueryIndex, in Position position) =>
            {
                // Find closest plant
                float distSq = float.MaxValue;
                Entity plant = Entity.Null;
                var lookupBuffer = lookupDataArray[lookup];
                var lookupEntityBuffer = lookupEntityArray[lookup];
                int2 plantPos = default;
                for (int i = 0; i < lookupBuffer.Length; i++)
                {
                    int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                    float newDistSq = math.lengthsq(position.Value - pos);
                    if (newDistSq < distSq && lookupBuffer[i].ComponentTypeIndex == plantIndex)
                    {
                        var newEntity = lookupEntityBuffer[i].Entity;
                        if (plantTag.HasComponent(newEntity) && plantTag[newEntity].Growth >= 1)
                        {
                            plant = newEntity;
                            distSq = newDistSq;
                            plantPos = pos;
                        }
                    }
                }

                if (plant == Entity.Null)
                {
                    cmdBuffer.RemoveComponent<WorkSellPlants>(entityInQueryIndex, e);
                }
                else
                {
                    cmdBuffer.AddComponent(entityInQueryIndex, e, new WorkTarget { Value = plant });
                    var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
                    buffer.Add(new PathData { Position = plantPos });
                }
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Does not carry plant, reached target
        if (!m_reachedPlantQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var plantTag = GetComponentDataFromEntity<PlantTag>(true);

            Entities.WithReadOnly(plantTag).WithAll<FarmerTag, WorkSellPlants, PathFinished>().WithNone<CarryingPlant>().WithStoreEntityQueryInField(ref m_reachedPlantQuery).ForEach((Entity e, int entityInQueryIndex, in WorkTarget target, in Position position) =>
            {
                // Carry plant
                if (plantTag.HasComponent(target.Value))
                {
                    cmdBuffer.AddComponent(entityInQueryIndex, e, new CarryingPlant { Seed = plantTag[target.Value].Seed });
                    cmdBuffer.DestroyEntity(entityInQueryIndex, target.Value);
                }
                cmdBuffer.RemoveComponent<WorkTarget>(entityInQueryIndex, e);
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Has plant, no target
        if (!m_findPlantQuery.IsEmptyIgnoreFilter)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();
            var lookup = GetSingletonEntity<LookupData>();
            var lookupDataArray = GetBufferFromEntity<LookupData>(true);
            var lookupEntityArray = GetBufferFromEntity<LookupEntity>(true);
            var storeTag = GetComponentDataFromEntity<StoreTag>(true);

            Entities.WithReadOnly(lookupDataArray).WithReadOnly(lookupEntityArray).WithReadOnly(storeTag).WithAll<FarmerTag, WorkSellPlants, CarryingPlant>().WithNone<WorkTarget>().WithStoreEntityQueryInField(ref m_findStoreQuery).ForEach((Entity e, int entityInQueryIndex, in Position position) =>
            {
                // Find closest store
                float distSq = float.MaxValue;
                Entity store = Entity.Null;
                var lookupBuffer = lookupDataArray[lookup];
                var lookupEntityBuffer = lookupEntityArray[lookup];
                int2 storePos = default;
                for (int i = 0; i < lookupBuffer.Length; i++)
                {
                    int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                    float newDistSq = math.lengthsq(position.Value - pos);
                    if (newDistSq < distSq && lookupBuffer[i].ComponentTypeIndex == storeIndex)
                    {
                        var newStore = lookupEntityBuffer[i].Entity;
                        if (storeTag.HasComponent(newStore))
                        {
                            store = newStore;
                            distSq = newDistSq;
                            storePos = pos;
                        }
                    }
                }

                if (store == Entity.Null)
                {
                    cmdBuffer.RemoveComponent<WorkSellPlants>(entityInQueryIndex, e);
                }
                else
                {
                    cmdBuffer.AddComponent(entityInQueryIndex, e, new WorkTarget { Value = store });
                    var buffer = cmdBuffer.AddBuffer<PathData>(entityInQueryIndex, e);
                    buffer.Add(new PathData { Position = storePos });
                }
            }).ScheduleParallel();

            m_cmdSystem.AddJobHandleForProducer(Dependency);
        }

        // Reached target
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

            Entities.WithAll<WorkSellPlants, CarryingPlant, WorkTarget>().WithAll<PathFinished>().WithStoreEntityQueryInField(ref m_sellPlantQuery).ForEach((Entity e, int entityInQueryIndex, ref RandomState rng, in WorkTarget target, in Position position) =>
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