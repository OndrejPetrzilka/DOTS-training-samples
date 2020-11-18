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
    static readonly Type[] m_jobs = new Type[] { typeof(WorkClearRocks), typeof(WorkTillGround), typeof(WorkPlantSeeds), typeof(WorkSellPlants) };

    public Text Text;
    public Text JobsText;

    float timeScale;
    int version;
    int maxJob;

    public int MaxJob
    {
        get { return FarmerDecision.MaxJob; }
        set { FarmerDecision.MaxJob = value; }
    }

    private void Update()
    {
        if (Time.timeScale != timeScale || version != StatsSystem.Version)
        {
            timeScale = Time.timeScale;
            version = StatsSystem.Version;
            Text.text = $"Time scale: {timeScale}\r\nFarmers: {StatsSystem.FarmerCount}\r\nRocks: {StatsSystem.RockCount}\r\nPlants: {StatsSystem.PlantCount}\r\nMoney: {StatsSystem.Money}";
        }
        if (maxJob != FarmerDecision.MaxJob)
        {
            maxJob = FarmerDecision.MaxJob;
            string jobs = m_jobs.Take(MaxJob).Select(s => s.Name).Aggregate((a, b) => a + "\r\n  " + b);
            JobsText.text = $"Jobs:\r\n  {jobs}";
        }
    }
}
