using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

public static class AddComponentHelper
{
    //struct SetDataJob : IJobChunk
    //{
    //    [DeallocateOnJobCompletion]
    //    public NativeArray<byte> Data;
    //    public DynamicComponentTypeHandle Handle;

    //    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //    {
    //        int size = Data.Length;
    //        var components = chunk.GetDynamicComponentDataArrayReinterpret<byte>(Handle, size);
    //        for (int i = 0; i < chunk.Count; i++)
    //        {
    //        }
    //    }
    //}

    public static JobHandle AddComponentJob<T>(this EntityManager manager, EntityCommandBuffer.ParallelWriter buffer, EntityQuery query, T data, JobHandle dependsOn = default)
        where T : struct, IComponentData
    {
        AddGenericComponentJob<T> job;
        job.ComponentData = data;
        job.CmdBuffer = buffer;
        job.Entities = manager.GetEntityTypeHandle();
        return job.ScheduleParallel(query, dependsOn);
    }

    public static JobHandle AddComponentJob<T>(this EntityCommandBufferSystem cmdBufferSystem, EntityQuery query, T data, JobHandle dependsOn = default)
        where T : struct, IComponentData
    {
        if (!query.IsEmpty)
        {
            //SetDataJob job;
            //job.Data = new NativeArray<byte>(UnsafeUtility.SizeOf<T>(), Allocator.TempJob);
            //job.Handle = cmdBufferSystem.EntityManager.GetDynamicComponentTypeHandle(typeof(T));
            //job.Data.ReinterpretStore(0, data);
            //var result = job.ScheduleParallel(query, dependsOn);

            var cmdBuffer = cmdBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var result = AddComponentJob(cmdBufferSystem.EntityManager, cmdBuffer, query, data, dependsOn);
            cmdBufferSystem.AddJobHandleForProducer(result);
            return result;
        }
        return dependsOn;
    }
}
