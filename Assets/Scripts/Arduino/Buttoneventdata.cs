using System;
using UnityEngine;

/// <summary>
/// Daten-Strukturen für Arduino Button Events
/// </summary>

/// <summary>
/// Button Event von Arduino
/// </summary>
[Serializable]
public class ButtonEventMessage
{
    public string type;        // "button_event"
    public int button_id;      // 1-5
    public string action;      // "pressed" oder "released"
    public float timestamp;    // Python Timestamp
}