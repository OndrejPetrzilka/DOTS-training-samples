using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class WorldGeneratorSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<GenerateWorld>();
    }

    protected override void OnUpdate()
    {
        var generateWorld = GetSingletonEntity<GenerateWorld>();
        var data = EntityManager.GetComponentData<GenerateWorld>(generateWorld);
        var settings = LoadSettings(generateWorld);

        uint seed = data.Seed != 0 ? (uint)data.Seed : (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        Generate(settings, EntityManager, seed);

        EntityManager.RemoveComponent<GenerateWorld>(generateWorld);
    }

    Settings LoadSettings(Entity entity)
    {
        var type = typeof(Settings).Assembly.GetType($"{nameof(Settings)}Authoring");
        var instance = (IConvertGameObjectToEntity)Object.FindObjectOfType(type);
        instance.Convert(entity, EntityManager, null);
        return EntityManager.GetComponentData<Settings>(entity);
    }

    private static void Generate(Settings settings, EntityManager manager, uint seed)
    {
        Random rng = new Random(seed);

        // Ground
        var ground = manager.CreateEntity();
        var buffer = manager.AddBuffer<Ground>(ground);
        buffer.Length = settings.mapSize.x * settings.mapSize.y;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = new Ground { Till = rng.NextFloat() * 0.2f };
        }

        // Stores
        var storeArch = manager.CreateArchetype(typeof(StoreTag), typeof(Position));
        bool[,] stores = new bool[settings.mapSize.x, settings.mapSize.y];
        int spawnedStores = 0;
        while (spawnedStores < settings.storeCount)
        {
            int x = rng.NextInt(0, settings.mapSize.x);
            int y = rng.NextInt(0, settings.mapSize.y);
            if (stores[x, y] == false)
            {
                var store = manager.CreateEntity(storeArch);
                manager.SetComponentData(store, new Position { Value = new int2(x, y) });
                manager.SetName(store, $"Store {spawnedStores}");

                stores[x, y] = true;
                spawnedStores++;
            }
        }

        // Rocks
        var rockArchetype = manager.CreateArchetype(typeof(RockTag), typeof(Position), typeof(Depth), typeof(Size), typeof(Health));
        bool[,] rocks = new bool[settings.mapSize.x, settings.mapSize.y];

        for (int i = 0; i < settings.rockSpawnAttempts; i++)
        {
            int width = rng.NextInt(0, 4);
            int height = rng.NextInt(0, 4);
            int rockX = rng.NextInt(0, settings.mapSize.x - width);
            int rockY = rng.NextInt(0, settings.mapSize.y - height);
            RectInt rect = new RectInt(rockX, rockY, width, height);

            bool blocked = false;
            for (int x = rockX; x <= rockX + width; x++)
            {
                for (int y = rockY; y <= rockY + height; y++)
                {
                    if (rocks[x, y] || stores[x, y])
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) break;
            }
            if (blocked == false)
            {
                var entity = manager.CreateEntity(rockArchetype);
                manager.SetComponentData(entity, new Position { Value = new int2(rockX, rockY) });
                manager.SetComponentData(entity, new Size { Value = new int2(width, height) });
                manager.SetComponentData(entity, new Health { Value = (rect.width + 1) * (rect.height + 1) * 15 });
                manager.SetComponentData(entity, new Depth { Value = rng.NextFloat(.4f, .8f) });
                manager.SetName(entity, $"Rock {i}");
                for (int x = rockX; x <= rockX + width; x++)
                {
                    for (int y = rockY; y <= rockY + height; y++)
                    {
                        rocks[x, y] = true;
                    }
                }
            }
        }

        // Farmers
        var farmerArchetype = manager.CreateArchetype(typeof(FarmerTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));

        int farmerCount = 0;
        while (farmerCount < settings.initialFarmerCount)
        {
            var spawnPos = new int2(rng.NextInt(0, settings.mapSize.x), rng.NextInt(0, settings.mapSize.y));
            if (!stores[spawnPos.x, spawnPos.y] && !rocks[spawnPos.x, spawnPos.y])
            {
                var pos = new float2(spawnPos.x + 0.5f, spawnPos.y + 0.5f);
                var farmer = manager.CreateEntity(farmerArchetype);
                manager.SetName(farmer, $"Farmer {farmerCount}");
                manager.SetComponentData(farmer, new Position { Value = pos });
                manager.SetComponentData(farmer, new SmoothPosition { Value = pos });
                farmerCount++;
            }
        }
    }
}