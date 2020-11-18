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
    public Toggle WorkToggle;

    float timeScale;
    int version;
    uint jobMask;

    Toggle[] m_toggles;

    void Awake()
    {
        m_toggles = new Toggle[m_jobs.Length];
        var container = WorkToggle.transform.parent;
        for (int i = 0; i < m_jobs.Length; i++)
        {
            var toggle = i == 0 ? WorkToggle : Instantiate(WorkToggle, container);

            var job = m_jobs[i];
            var text = toggle.GetComponentInChildren<Text>();
            text.text = job.Name;
            int index = i;
            toggle.onValueChanged.AddListener(enabled => OnValueChanged(index, enabled));

            m_toggles[i] = toggle;
        }
        UpdateValues();
    }

    void Update()
    {
        if (Time.timeScale != timeScale || version != StatsSystem.Version)
        {
            timeScale = Time.timeScale;
            version = StatsSystem.Version;
            Text.text = $"Time scale: {timeScale}\r\nFarmers: {StatsSystem.FarmerCount}\r\nRocks: {StatsSystem.RockCount}\r\nPlants: {StatsSystem.PlantCount}\r\nMoney: {StatsSystem.Money}";
        }
        if (jobMask != FarmerDecision.JobMask)
        {
            jobMask = FarmerDecision.JobMask;
            UpdateValues();
        }
    }

    public void EnableEverything(bool enable)
    {
        FarmerDecision.JobMask = enable ? uint.MaxValue : 0;
    }

    void UpdateValues()
    {
        for (int i = 0; i < m_toggles.Length; i++)
        {
            m_toggles[i].SetIsOnWithoutNotify((FarmerDecision.JobMask & (1u << i)) != 0);
        }
    }

    private void OnValueChanged(int jobIndex, bool enabled)
    {
        if (enabled)
        {
            FarmerDecision.JobMask |= (1u << jobIndex);
        }
        else
        {
            FarmerDecision.JobMask &= ~(1u << jobIndex);
        }
    }
}
