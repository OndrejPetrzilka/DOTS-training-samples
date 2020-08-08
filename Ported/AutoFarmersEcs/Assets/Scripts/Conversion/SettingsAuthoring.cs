using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class SettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public int2 mapSize;
    public int storeCount;
    public int rockSpawnAttempts;
    public Mesh rockMesh;
    public Material rockMaterial;
    public Material plantMaterial;
    public Mesh groundMesh;
    public Material groundMaterial;
    public Mesh storeMesh;
    public Material storeMaterial;
    public AnimationCurve soldPlantYCurve;
    public AnimationCurve soldPlantXZScaleCurve;
    public AnimationCurve soldPlantYScaleCurve;

    [Space(10f)]
    public Mesh farmerMesh;
    public Material farmerMaterial;
    public int initialFarmerCount;
    public int maxFarmerCount;
    [Range(0f, 1f)]
    public float movementSmooth;

    [Space(10f)]
    public Mesh droneMesh;
    public Material droneMaterial;
    public int maxDroneCount;
    [Range(0f, 1f)]
    public float moveSmooth;
    [Range(0f, 1f)]
    public float carrySmooth;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Settings settings = new Settings();
        settings.mapSize = mapSize;
        settings.storeCount = storeCount;
        settings.rockSpawnAttempts = rockSpawnAttempts;
        settings.rockMesh = rockMesh;
        settings.rockMaterial = rockMaterial;
        settings.plantMaterial = plantMaterial;
        settings.groundMesh = groundMesh;
        settings.groundMaterial = groundMaterial;
        settings.storeMesh = storeMesh;
        settings.storeMaterial = storeMaterial;
        settings.soldPlantYCurve = soldPlantYCurve;
        settings.soldPlantXZScaleCurve = soldPlantXZScaleCurve;
        settings.soldPlantYScaleCurve = soldPlantYScaleCurve;
        settings.farmerMesh = farmerMesh;
        settings.farmerMaterial = farmerMaterial;
        settings.initialFarmerCount = initialFarmerCount;
        settings.maxFarmerCount = maxFarmerCount;
        settings.movementSmooth = movementSmooth;
        settings.droneMesh = droneMesh;
        settings.droneMaterial = droneMaterial;
        settings.maxDroneCount = maxDroneCount;
        settings.moveSmooth = moveSmooth;
        settings.carrySmooth = carrySmooth;
        EntityManagerManagedComponentExtensions.AddComponentData(dstManager, entity, settings);

        Generate(dstManager, conversionSystem);
    }

    private void Generate(EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Ground
        var ground = dstManager.CreateEntity();
        var buffer = dstManager.AddBuffer<Ground>(ground);
        buffer.Length = mapSize.x * mapSize.y;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = new Ground { Till = Random.value * 0.2f };
        }

        // Stores
        var storeArch = dstManager.CreateArchetype(typeof(StoreTag), typeof(Position));
        bool[,] stores = new bool[mapSize.x, mapSize.y];
        int spawnedStores = 0;
        while (spawnedStores < storeCount)
        {
            int x = Random.Range(0, mapSize.x);
            int y = Random.Range(0, mapSize.y);
            if (stores[x, y] == false)
            {
                var store = dstManager.CreateEntity(storeArch);
                dstManager.SetComponentData(store, new Position { Value = new int2(x, y) });
                dstManager.SetName(store, $"Store {spawnedStores}");

                stores[x, y] = true;
                spawnedStores++;
            }
        }

        // Rocks
        var rockArchetype = dstManager.CreateArchetype(typeof(RockTag), typeof(Position), typeof(Depth), typeof(Size), typeof(Health));
        bool[,] rocks = new bool[mapSize.x, mapSize.y];

        for (int i = 0; i < rockSpawnAttempts; i++)
        {
            int width = Random.Range(0, 4);
            int height = Random.Range(0, 4);
            int rockX = Random.Range(0, mapSize.x - width);
            int rockY = Random.Range(0, mapSize.y - height);
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
                var entity = dstManager.CreateEntity(rockArchetype);
                dstManager.SetComponentData(entity, new Position { Value = new int2(rockX, rockY) });
                dstManager.SetComponentData(entity, new Size { Value = new int2(width, height) });
                dstManager.SetComponentData(entity, new Health { Value = (rect.width + 1) * (rect.height + 1) * 15 });
                dstManager.SetComponentData(entity, new Depth { Value = Random.Range(.4f, .8f) });
                dstManager.SetName(entity, $"Rock {i}");
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
        var farmerArchetype = dstManager.CreateArchetype(typeof(FarmerTag), typeof(Position), typeof(SmoothPosition), typeof(Offset));

        int farmerCount = 0;
        while (farmerCount < initialFarmerCount)
        {
            var spawnPos = new int2(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
            if (!stores[spawnPos.x, spawnPos.y] && !rocks[spawnPos.x, spawnPos.y])
            {
                var pos = new float2(spawnPos.x + 0.5f, spawnPos.y + 0.5f);
                var farmer = dstManager.CreateEntity(farmerArchetype);
                dstManager.SetName(farmer, $"Farmer {farmerCount}");
                dstManager.SetComponentData(farmer, new Position { Value = pos });
                dstManager.SetComponentData(farmer, new SmoothPosition { Value = pos });
                farmerCount++;
            }
        }
    }
}