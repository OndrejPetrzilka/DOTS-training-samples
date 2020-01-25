using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class FixedUpdateSimulation
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MoveSimulationGroup()
    {
        // This must be called AFTER DefaultWorldInitialization, otherwise DefaultWorldInitialization overwrites PlayerLoop
        var playerLoop = ScriptBehaviourUpdateOrder.CurrentPlayerLoop;
        var func = RemoveCallback<SimulationSystemGroup>(playerLoop);
        if (func != null)
        {
            InstallCallback<SimulationSystemGroup>(playerLoop, typeof(FixedUpdate), func);
            ScriptBehaviourUpdateOrder.SetPlayerLoop(playerLoop);
        }
    }

    static void InstallCallback<T>(PlayerLoopSystem playerLoop, Type subsystem, PlayerLoopSystem.UpdateFunction callback)
    {
        for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
        {
            int subsystemListLength = playerLoop.subSystemList[i].subSystemList.Length;
            if (playerLoop.subSystemList[i].type == subsystem)
            {
                // Create new subsystem list and add callback
                var newSubsystemList = new PlayerLoopSystem[subsystemListLength + 1];
                for (var j = 0; j < subsystemListLength; j++)
                {
                    newSubsystemList[j] = playerLoop.subSystemList[i].subSystemList[j];
                }
                newSubsystemList[subsystemListLength].type = typeof(FixedUpdateSimulation);
                newSubsystemList[subsystemListLength].updateDelegate = callback;
                playerLoop.subSystemList[i].subSystemList = newSubsystemList;
            }
        }
    }

    static PlayerLoopSystem.UpdateFunction RemoveCallback<T>(PlayerLoopSystem playerLoop)
    {
        for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
        {
            int subsystemListLength = playerLoop.subSystemList[i].subSystemList.Length;
            for (var j = 0; j < subsystemListLength; j++)
            {
                var item = playerLoop.subSystemList[i].subSystemList[j];
                if (item.type == typeof(T))
                {
                    playerLoop.subSystemList[i].subSystemList = ExceptIndex(playerLoop.subSystemList[i].subSystemList, j);
                    return item.updateDelegate;
                }
            }
        }
        return null;
    }

    static T[] ExceptIndex<T>(T[] array, int exceptIndex)
    {
        T[] result = new T[array.Length - 1];
        if (exceptIndex > 0)
        {
            Array.Copy(array, result, exceptIndex);
        }
        if (exceptIndex < array.Length - 1)
        {
            Array.Copy(array, exceptIndex + 1, result, exceptIndex, array.Length - exceptIndex - 1);
        }
        return result;
    }
}