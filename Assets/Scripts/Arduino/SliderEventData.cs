using System;
using UnityEngine;

/// <summary>
/// Slider Event Data von Arduino/Python
/// </summary>
[Serializable]
public class SliderEventMessage
{
    public string type;         // "slider_event"
    public int slider_id;       // 1 oder 2
    public float value;         // 0.0 - 1.0
    public float timestamp;     // Python Timestamp
}