using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// Adds <see cref="LookupComponent"/> to components entities with specific component.
/// </summary>
[UpdateInGroup(typeof(LookupGroup))]
[DisableAutoCreation]
public class LookupRegistrationSystem_obsolete : SystemBase
{
    public struct AddJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter CmdBuffer;

        [ReadOnly]
        public EntityTypeHandle Entity;

        public int ComponentIndex;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var entities = chunk.GetNativeArray(Entity);

            for (int i = 0; i < chunk.Count; i++)
            {
                CmdBuffer.AddComponent(firstEntityIndex + i, entities[i], new LookupComponent(ComponentIndex));
            }
        }
    }

    struct Element
    {
        public EntityQuery AddedQuery;
        public ComponentType ComponentType;
        public int ComponentTypeIndex;
    }

    EntityCommandBufferSystem m_cmdSystem;
    List<Element> m_elements = new List<Element>();
    EntityQuery m_deletedQuery;

    public void Register(Type componentType)
    {
        Element element;
        element.ComponentType = componentType;
        element.ComponentTypeIndex = TypeManager.GetTypeIndex(componentType);
        element.AddedQuery = Query.WithAll(componentType).WithNone<LookupComponent>();
        m_elements.Add(element);

        m_deletedQuery = Query.WithAll<LookupComponent>().WithNone(m_elements.Select(s => s.ComponentType).ToArray());
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_cmdSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();

        Register(typeof(RockTag));
        Register(typeof(StoreTag));
        Register(typeof(PlantTag));
    }

    protected override void OnUpdate()
    {
        if (!m_deletedQuery.Equals(default))
        {
            m_cmdSystem.CreateCommandBuffer().RemoveComponent<LookupComponent>(m_deletedQuery);
        }

        // TODO: Optimize
        // Adding could be optimized, ComponentTypeIndex would not be probably necessary
        // It has same value for whole chunk, SharedComponent or ChunkComponent could be used probably

        int index = 0;
        NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(m_elements.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        foreach (var element in m_elements)
        {
            var cmdBuffer = m_cmdSystem.CreateCommandBuffer().AsParallelWriter();

            AddJob addJob;
            addJob.CmdBuffer = cmdBuffer;
            addJob.Entity = GetEntityTypeHandle();
            addJob.ComponentIndex = element.ComponentTypeIndex;
            var handle = addJob.ScheduleParallel(element.AddedQuery, Dependency);
            dependencies[index] = handle;
            index++;
        }
        Dependency = JobHandle.CombineDependencies(dependencies);
        m_cmdSystem.AddJobHandleForProducer(Dependency);
    }
}
