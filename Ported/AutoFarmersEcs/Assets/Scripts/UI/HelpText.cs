using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HelpText : MonoBehaviour
{
    public Text Text;

    float timeScale;

    private void Update()
    {
        if (Time.timeScale != timeScale)
        {
            timeScale = Time.timeScale;
            Text.text = $"Time scale: {timeScale}";
        }
    }
}
