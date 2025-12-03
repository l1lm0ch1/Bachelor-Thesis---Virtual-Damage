using System;
using UnityEngine;

/// <summary>
/// Daten-Strukturen für Tracking Messages von Python
/// Müssen exakt mit Python JSON Format übereinstimmen!
/// </summary>

/// <summary>
/// Object Update Message von Python
/// </summary>
[Serializable]
public class ObjectUpdateMessage
{
    public string type;              // "object_update"
    public string object_key;        // z.B. "A1", "B1"
    public string object_name;       // z.B. "Objekt A1"
    public string object_type;       // "A" oder "B"
    public int marker_id;            // ArUco Marker ID
    public string side;              // "OBEN", "UNTEN", etc.
    public PositionData position;    // 3D Position in Meter
    public RotationData rotation;    // 3D Rotation in Grad
    public float timestamp;          // Python Timestamp
}

/// <summary>
/// Position Daten (in Meter, Unity Koordinaten)
/// </summary>
[Serializable]
public class PositionData
{
    public float x;  // Rechts (+) / Links (-)
    public float y;  // Hoch (+) / Runter (-)
    public float z;  // Vorwärts (+) / Zurück (-)

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// Rotation Daten (in Grad, Euler Angles)
/// </summary>
[Serializable]
public class RotationData
{
    public float roll;   // Rotation um Z-Achse
    public float pitch;  // Rotation um X-Achse
    public float yaw;    // Rotation um Y-Achse

    public Quaternion ToQuaternion()
    {
        // Euler zu Quaternion: Reihenfolge Y->X->Z
        return Quaternion.Euler(pitch, yaw, roll);
    }

    public Vector3 ToEulerAngles()
    {
        return new Vector3(pitch, yaw, roll);
    }
}

/// <summary>
/// Object Lost Message von Python
/// </summary>
[Serializable]
public class ObjectLostMessage
{
    public string type;         // "object_lost"
    public string object_key;   // z.B. "A1"
}

/// <summary>
/// RFID Event Message (später für Arduino Integration)
/// </summary>
[Serializable]
public class RFIDEventMessage
{
    public string type;         // "rfid_event"
    public string object_key;   // z.B. "A1"
    public string zone;         // "ZONE_A" oder "ZONE_B"
    public bool is_correct;     // Richtig platziert?
}

/// <summary>
/// Objekt-Konfiguration (Prefab Mapping)
/// </summary>
[Serializable]
public class TrackedObjectConfig
{
    public string objectKey;       // z.B. "A1", "B1"
    public string objectType;      // "A" oder "B"
    public GameObject prefab;      // Zugewiesenes Prefab
    public float size;             // Größe in Meter (z.B. 0.07 für 7cm)
}