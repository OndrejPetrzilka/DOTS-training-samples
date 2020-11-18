using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

public struct AddGenericComponentJob<T> : IJobChunk
        where T : struct, IComponentData
{
    public T ComponentData;
    public EntityCommandBuffer.ParallelWriter CmdBuffer;

    [ReadOnly]
    public EntityTypeHandle Entities;

    public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    {
        var entities = chunk.GetNativeArray(Entities);
        for (int i = 0; i < chunk.Count; i++)
        {
            CmdBuffer.AddComponent(firstEntityIndex, entities[i], ComponentData);
        }
    }
}