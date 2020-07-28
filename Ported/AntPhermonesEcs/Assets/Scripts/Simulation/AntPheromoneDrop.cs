using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(AntSteering))]
public class AntPheromoneDrop : JobComponentSystem
{
    AntSettings m_settings;
    AntPheromones m_pheromones;
    AntPheromoneSteering m_steering;
    JobHandle m_handle;

    public JobHandle Handle
    {
        get { return m_handle; }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_settings = AntSettingsManager.Current;
        m_pheromones = World.GetExistingSystem<AntPheromones>();
        m_steering = World.GetExistingSystem<AntPheromoneSteering>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int mapSize = m_settings.mapSize;
        float antSpeed = m_settings.antSpeed;
        float trailAddSpeed = m_settings.trailAddSpeed;
        float deltaTime = UnityEngine.Time.fixedDeltaTime;
        var pheromones = m_pheromones.Pheromones;

        inputDeps = JobHandle.CombineDependencies(inputDeps, m_steering.Handle);

        var withResource = Entities.WithName("DropR").WithAll<AntTag, HoldingResourceTag>().ForEach((Entity e, ref Position position, ref Speed speed) =>
        {
            float excitement = 1f;
            excitement *= speed.Value / antSpeed;

            int x = Mathf.FloorToInt(position.Value.x);
            int y = Mathf.FloorToInt(position.Value.y);
            if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
            {
                return;
            }

            int index = AntPheromones.PheromoneIndex(x, y, mapSize);
            var value = pheromones[index];
            value += (trailAddSpeed * excitement * deltaTime) * (1f - pheromones[index]);
            if (value > 1f)
            {
                value = 1f;
            }
            pheromones[index] = value;
        }).Schedule(inputDeps);

        m_handle = Entities.WithName("DropN").WithAll<AntTag>().WithNone<HoldingResourceTag>().ForEach((Entity e, ref Position position, ref Speed speed) =>
        {
            float excitement = .3f;
            excitement *= speed.Value / antSpeed;

            int x = Mathf.FloorToInt(position.Value.x);
            int y = Mathf.FloorToInt(position.Value.y);
            if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
            {
                return;
            }

            int index = AntPheromones.PheromoneIndex(x, y, mapSize);
            var value = pheromones[index];
            value += (trailAddSpeed * excitement * deltaTime) * (1f - pheromones[index]);
            if (value > 1f)
            {
                value = 1f;
            }
            pheromones[index] = value;
        }).Schedule(withResource);

        return m_handle;
    }
}