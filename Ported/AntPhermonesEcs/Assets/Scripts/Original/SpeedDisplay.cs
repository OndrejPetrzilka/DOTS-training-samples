using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplay : MonoBehaviour
{
    public Text Text;

    private float m_scale = 1;

    private void OnEnable()
    {
        UpdateDisplay();
    }

    private void Update()
    {
        if (m_scale != Time.timeScale)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        m_scale = Time.timeScale;
        Text.text = $"Time.timeScale: {m_scale:0.0}";
    }
}
