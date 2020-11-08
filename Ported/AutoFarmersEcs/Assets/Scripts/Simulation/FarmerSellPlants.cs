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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public class FarmerSellPlants : SystemBase
{
    EntityQuery m_hasWork;
    EntityQuery m_farmers;
    EntityArchetype m_plantArchetype;
    EntityArchetype m_farmerArchetype;
    EntityCommandBufferSystem m_cmdBufferSystem;

    int m_money;

    public int Money
    {
        get { return m_money; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<Settings>();
        RequireSingletonForUpdate<Ground>();
        RequireSingletonForUpdate<RockLookup>();
        RequireSingletonForUpdate<StoreLookup>();
        RequireSingletonForUpdate<PlantLookup>();
        m_hasWork = GetEntityQuery(typeof(WorkSellPlants));
        m_farmers = GetEntityQuery(typeof(FarmerTag));
        m_plantArchetype = EntityManager.CreateArchetype(typeof(PlantTag), typeof(Position));
        m_farmerArchetype = EntityManager.CreateArchetype(typeof(FarmerTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));
        m_cmdBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var settings = this.GetSettings();
        var mapSize = settings.mapSize;
        var maxFarmerCount = settings.maxFarmerCount;

        var ground = GetBuffer<Ground>(GetSingletonEntity<Ground>());
        var plants = this.GetSingleton<PlantLookup>();

        // TODO: Use command buffer

        // Does not carry plant, find plant
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkSellPlants>().WithNone<CarryingPlant, WorkTarget>().ForEach((Entity e, in Position position) =>
        {
            // Find closest plant
            float distSq = float.MaxValue;
            Entity plant = Entity.Null;
            for (int i = 0; i < plants.Length; i++)
            {
                int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                float newDistSq = math.lengthsq(position.Value - pos);
                if (newDistSq < distSq)
                {
                    var newEntity = plants[i].Entity;
                    if (EntityManager.Exists(newEntity) && EntityManager.GetComponentData<PlantTag>(newEntity).Growth >= 1)
                    {
                        plant = newEntity;
                        distSq = newDistSq;
                    }
                }
            }

            if (plant == Entity.Null)
            {
                EntityManager.RemoveComponent(e, typeof(WorkSellPlants));
            }
            else
            {
                EntityManager.AddComponentData(e, new WorkTarget { Value = plant });
                var buffer = EntityManager.AddBuffer<PathData>(e);
                buffer.Add(new PathData { Position = EntityManager.GetComponentData<Position>(plant).Value });
            }
        }).Run();

        // Does not carry plant, reached target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkSellPlants, PathFinished>().WithNone<CarryingPlant>().ForEach((Entity e, in WorkTarget target, in Position position) =>
        {
            // Carry plant
            if (EntityManager.Exists(target.Value))
            {
                EntityManager.AddComponentData(e, new CarryingPlant { Seed = EntityManager.GetComponentData<PlantTag>(target.Value).Seed });
                EntityManager.DestroyEntity(target.Value);
            }
            EntityManager.RemoveComponent<WorkTarget>(e);
        }).Run();

        // In job below it was invalid wihout this refresh, not sure why
        var stores = this.GetSingleton<StoreLookup>();

        // Has plant, no target
        Entities.WithStructuralChanges().WithAll<FarmerTag, WorkSellPlants, CarryingPlant>().WithNone<WorkTarget>().ForEach((Entity e, in Position position) =>
        {
            // Find closest store
            float distSq = float.MaxValue;
            Entity store = Entity.Null;
            for (int i = 0; i < stores.Length; i++)
            {
                int2 pos = new int2(i % mapSize.x, i / mapSize.x);
                float newDistSq = math.lengthsq(position.Value - pos);
                if (newDistSq < distSq)
                {
                    var newStore = stores[i].Entity;
                    if (EntityManager.Exists(newStore))
                    {
                        store = newStore;
                        distSq = newDistSq;
                    }
                }
            }

            if (store == Entity.Null)
            {
                EntityManager.RemoveComponent(e, typeof(WorkPlantSeeds));
            }
            else
            {
                EntityManager.AddComponentData(e, new WorkTarget { Value = store });
                var buffer = EntityManager.AddBuffer<PathData>(e);
                buffer.Add(new PathData { Position = EntityManager.GetComponentData<Position>(store).Value });
            }
        }).Run();

        int farmerCount = m_farmers.CalculateEntityCount();

        // Reached target
        var cmdBuffer = m_cmdBufferSystem.CreateCommandBuffer();
        Entities.WithStructuralChanges().WithAll<WorkSellPlants, CarryingPlant, WorkTarget>().WithAll<PathFinished>().ForEach((Entity e, in WorkTarget target, in Position position) =>
        {
            // Sell plant

            // TODO: Get money and add new farmer / drone
            m_money++;
            if (farmerCount < maxFarmerCount && m_money >= 10)
            {
                m_money -= 10;

                var pos = position.Value;
                var farmer = cmdBuffer.CreateEntity(m_farmerArchetype);
                //cmdBuffer.SetName(farmer, $"Farmer {farmerCount}");
                cmdBuffer.SetComponent(farmer, new Position { Value = pos });
                cmdBuffer.SetComponent(farmer, new SmoothPosition { Value = pos });
                farmerCount++;
            }

            // Remove target
            cmdBuffer.RemoveComponent(e, typeof(WorkTarget));
            cmdBuffer.RemoveComponent(e, typeof(CarryingPlant));

            // Choose other work
            if (Random.value < 0.1f)
            {
                cmdBuffer.RemoveComponent(e, typeof(WorkSellPlants));
            }
        }).Run();

        m_cmdBufferSystem.AddJobHandleForProducer(Dependency);
    }
}