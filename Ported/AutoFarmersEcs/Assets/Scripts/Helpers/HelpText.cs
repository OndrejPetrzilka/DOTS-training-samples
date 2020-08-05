using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class HelpText : MonoBehaviour
{
    public Text Text;

    float timeScale;
    int version;

    private void Update()
    {
        if (Time.timeScale != timeScale || version != StatsSystem.Version)
        {
            timeScale = Time.timeScale;
            version = StatsSystem.Version;
            Text.text = $"Time scale: {timeScale}\r\nFarmers: {StatsSystem.FarmerCount}\r\nRocks: {StatsSystem.RockCount}\r\nPlants: {StatsSystem.PlantCount}";
        }
    }
}
