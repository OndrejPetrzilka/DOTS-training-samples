using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PathHelper
{
    [ReadOnly]
    DynamicBuffer<Ground> Ground;
    [ReadOnly]
    DynamicBuffer<LookupData> Lookup;
    int2 StartPosition;
    int2 MapSize;
    int NonWalkableComponentIndex;
    int TargetComponentIndex;
    byte TargetFilters;
    bool Flying;
    bool CheckGroundState;
    bool GroundStateTilled;
    DynamicBuffer<PathData> Path;

    public static void FindPath(DynamicBuffer<Ground> ground, DynamicBuffer<LookupData> lookup, int2 startPosition, int2 mapSize, int nonWalkableComponentIndex, FindPath search, DynamicBuffer<PathData> path)
    {
        PathHelper helper;
        helper.Ground = ground;
        helper.Lookup = lookup;
        helper.StartPosition = startPosition;
        helper.MapSize = mapSize;
        helper.NonWalkableComponentIndex = nonWalkableComponentIndex;
        helper.TargetComponentIndex = search.ComponentTypeIndex;
        helper.TargetFilters = search.Filters;
        helper.Flying = (search.Flags & FindPathFlags.Flying) != 0;
        helper.CheckGroundState = (search.Flags & FindPathFlags.UseGroundState) != 0;
        helper.GroundStateTilled = (search.Flags & FindPathFlags.GroundStateTilled) != 0;
        helper.Path = path;
        helper.Search();
    }

    private void Search()
    {
        NativeArray<ushort> marks = new NativeArray<ushort>(MapSize.x * MapSize.y, Allocator.Temp);
        NativeList<int3> openQueue1 = new NativeList<int3>(32, Allocator.Temp);
        NativeList<int3> openQueue2 = new NativeList<int3>(32, Allocator.Temp);
        openQueue1.Add(new int3(StartPosition, 1));
        marks[GetIndex(StartPosition)] = 1;

        if (FindTarget(openQueue1, openQueue2, marks, out int2 position, out int distance))
        {
            Path.Length = 0;
            Path.Capacity = distance - 1;

            while (distance >= 1)
            {
                Path.Add(new PathData { Position = position });
                distance--;

                if (!BacktrackNeighbour(marks, distance, new int2(1, 0), ref position)
                   && !BacktrackNeighbour(marks, distance, new int2(-1, 0), ref position)
                   && !BacktrackNeighbour(marks, distance, new int2(0, 1), ref position)
                   && !BacktrackNeighbour(marks, distance, new int2(0, -1), ref position))
                {
                    // Error in algorithm
                    break;
                }
            }
        }
        openQueue1.Dispose();
        openQueue2.Dispose();
        marks.Dispose();
    }

    private bool BacktrackNeighbour(NativeArray<ushort> marks, int distance, int2 offset, ref int2 position)
    {
        var pos = position + offset;
        if (math.all(pos >= 0 & pos < MapSize))
        {
            int index = GetIndex(pos);
            if (marks[index] == distance)
            {
                position = pos;
                return true;
            }
        }
        return false;
    }

    private bool FindTarget(NativeList<int3> openQueue1, NativeList<int3> openQueue2, NativeArray<ushort> marks, out int2 position, out int distance)
    {
        // 1) Dequeue open list
        // 2) If target - break
        // 3) For each neighbour
        // 4) - if walkable and not marked, mark it and Enqueue to open list
        while (openQueue1.Length > 0)
        {
            // Process openQueue1, write neighbours to openQueue2
            for (int i = 0; i < openQueue1.Length; i++)
            {
                var item = openQueue1[i];
                int index = GetIndex(item.xy);
                if (IsTarget(index))
                {
                    position = item.xy;
                    distance = item.z;
                    return true;
                }

                AddNeighbour(openQueue2, marks, item, new int2(1, 0));
                AddNeighbour(openQueue2, marks, item, new int2(-1, 0));
                AddNeighbour(openQueue2, marks, item, new int2(0, 1));
                AddNeighbour(openQueue2, marks, item, new int2(0, -1));
            }

            openQueue1.Clear();
            var tmp = openQueue1;
            openQueue1 = openQueue2;
            openQueue2 = tmp;
        }
        position = default;
        distance = default;
        return false;
    }

    private void AddNeighbour(NativeList<int3> openQueue, NativeArray<ushort> marks, int3 item, int2 offset)
    {
        item.xy += offset;
        item.z++;
        if (math.all(item.xy >= 0 & item.xy < MapSize))
        {
            int index = GetIndex(item.xy);
            if (marks[index] == 0)
            {
                // Not visited, add
                if (IsTarget(index) || IsWalkable(index))
                {
                    marks[index] = (ushort)item.z;
                    openQueue.Add(item);
                }
            }
        }
    }

    private int GetIndex(int2 position)
    {
        return position.x + position.y * MapSize.x;
    }

    private bool IsTarget(int index)
    {
        if (CheckGroundState && Ground[index].IsTilled != GroundStateTilled)
            return false;

        return Lookup[index].Equals(TargetComponentIndex, TargetFilters);
    }

    private bool IsWalkable(int index)
    {
        return Flying || Lookup[index].ComponentTypeIndex != NonWalkableComponentIndex;
    }
}
