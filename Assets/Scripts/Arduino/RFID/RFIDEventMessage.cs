using System;
using UnityEngine;

/// <summary>
/// RFID Event von Arduino/Python
/// </summary>
[Serializable]
public class RFIDEventMessage
{
    public string type;        // "rfid_event"
    public string zone;        // "A" oder "B"
    public string uid;         // "AB:CD:EF:01"
    public string cube_id;     // "A1", "B2", etc.
    public string cube_type;   // "A" oder "B"
    public bool correct;       // true wenn richtige Zone
    public float timestamp;    // Python Timestamp
}