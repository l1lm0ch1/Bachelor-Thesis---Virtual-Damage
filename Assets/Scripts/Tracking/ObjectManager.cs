using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Manager - Verwaltet alle getrackten Objekte
/// Spawnt, updated und entfernt Objekte basierend auf Python Tracking Daten
/// 
/// WICHTIG: RFID Events werden NICHT mehr hier gehandelt!
/// RFID wird jetzt von ArduinoUDPReceiver + SortingTaskManager gehandelt.
/// </summary>
public class ObjectManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UDP Receiver GameObject")]
    public UDPReceiver udpReceiver;

    [Header("Prefabs")]
    [Tooltip("Prefab für Typ A Objekte (z.B. rote Würfel)")]
    public GameObject prefabTypeA;

    [Tooltip("Prefab für Typ B Objekte (z.B. blaue Würfel)")]
    public GameObject prefabTypeB;

    [Header("Tracking Settings")]
    [Tooltip("Position Smoothing (0 = keine, 1 = maximal)")]
    [Range(0f, 1f)]
    public float positionSmoothing = 0.3f;

    [Tooltip("Rotation Smoothing (0 = keine, 1 = maximal)")]
    [Range(0f, 1f)]
    public float rotationSmoothing = 0.3f;

    [Tooltip("Basis-Offset für alle Objekte (Tisch-Position anpassen)")]
    public Vector3 worldOffset = new Vector3(0, 0, 0);

    [Header("Visualization")]
    [Tooltip("Zeige Debug-Achsen an Objekten")]
    public bool showDebugAxes = true;

    [Tooltip("Zeige Objekt-Info als Text")]
    public bool showObjectLabels = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Dictionary: Object Key → GameObject Instance
    private Dictionary<string, TrackedObject> trackedObjects = new Dictionary<string, TrackedObject>();

    // Stats
    private int totalObjectsSpawned = 0;
    private int activeObjects = 0;

    void Start()
    {
        // UDP Receiver finden falls nicht zugewiesen
        if (udpReceiver == null)
        {
            udpReceiver = FindFirstObjectByType<UDPReceiver>();

            if (udpReceiver == null)
            {
                Debug.LogError("UDPReceiver nicht gefunden! Bitte zuweisen.");
                return;
            }
        }

        // Event subscriben
        udpReceiver.MessageReceived += OnMessageReceived;

        Debug.Log("<color=green>Object Manager initialisiert</color>");
        Debug.Log($"  Prefab Typ A: {(prefabTypeA != null ? prefabTypeA.name : "FEHLT!")}");
        Debug.Log($"  Prefab Typ B: {(prefabTypeB != null ? prefabTypeB.name : "FEHLT!")}");
    }

    void OnDestroy()
    {
        if (udpReceiver != null)
        {
            udpReceiver.MessageReceived -= OnMessageReceived;
        }
    }

    /// <summary>
    /// Callback wenn UDP Message empfangen wird
    /// RFID Events werden von ArduinoUDPReceiver gehandelt, nicht hier!
    /// </summary>
    private void OnMessageReceived(string messageType, string jsonData)
    {
        switch (messageType)
        {
            case "object_update":
                HandleObjectUpdate(jsonData);
                break;

            case "object_lost":
                HandleObjectLost(jsonData);
                break;

            default:
                if (showDebugLogs)
                {
                    Debug.LogWarning($"Unbekannter Message Type: {messageType}");
                }
                break;
        }
    }

    /// <summary>
    /// Verarbeitet Object Update (Position/Rotation Update oder Spawn)
    /// </summary>
    private void HandleObjectUpdate(string jsonData)
    {
        ObjectUpdateMessage msg = JsonUtility.FromJson<ObjectUpdateMessage>(jsonData);

        if (msg == null)
        {
            Debug.LogError("Fehler beim Parsen von Object Update Message");
            return;
        }

        // Existiert Objekt bereits?
        if (trackedObjects.ContainsKey(msg.object_key))
        {
            // Update existierendes Objekt
            UpdateTrackedObject(msg);
        }
        else
        {
            // Spawn neues Objekt
            SpawnTrackedObject(msg);
        }
    }

    /// <summary>
    /// Spawnt ein neues getrackt Objekt
    /// </summary>
    private void SpawnTrackedObject(ObjectUpdateMessage msg)
    {
        // Wähle richtiges Prefab
        GameObject prefab = msg.object_type == "A" ? prefabTypeA : prefabTypeB;

        if (prefab == null)
        {
            Debug.LogError($"Prefab für Typ {msg.object_type} nicht zugewiesen!");
            return;
        }

        // Position mit Offset
        Vector3 spawnPos = msg.position.ToVector3() + worldOffset;
        Quaternion spawnRot = msg.rotation.ToQuaternion();

        // Instantiate
        GameObject instance = Instantiate(prefab, spawnPos, spawnRot, transform);
        instance.name = msg.object_name;

        // TrackedObject Component hinzufügen
        TrackedObject trackedObj = instance.AddComponent<TrackedObject>();
        trackedObj.Initialize(msg, positionSmoothing, rotationSmoothing);

        // Debug Visualization
        if (showDebugAxes)
        {
            AddDebugAxes(instance);
        }

        if (showObjectLabels)
        {
            AddObjectLabel(instance, msg.object_name);
        }

        // Zu Dictionary hinzufügen
        trackedObjects.Add(msg.object_key, trackedObj);

        totalObjectsSpawned++;
        activeObjects++;

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>Spawned: {msg.object_name} (ID {msg.marker_id}, {msg.side})</color>");
        }
    }

    /// <summary>
    /// Updated ein existierendes Objekt
    /// </summary>
    private void UpdateTrackedObject(ObjectUpdateMessage msg)
    {
        TrackedObject trackedObj = trackedObjects[msg.object_key];

        if (trackedObj != null)
        {
            Vector3 targetPos = msg.position.ToVector3() + worldOffset;
            Quaternion targetRot = msg.rotation.ToQuaternion();

            trackedObj.UpdateTarget(targetPos, targetRot, msg);
        }
    }

    /// <summary>
    /// Entfernt ein Objekt das nicht mehr getrackt wird
    /// </summary>
    private void HandleObjectLost(string jsonData)
    {
        ObjectLostMessage msg = JsonUtility.FromJson<ObjectLostMessage>(jsonData);

        if (msg == null || !trackedObjects.ContainsKey(msg.object_key))
        {
            return;
        }

        TrackedObject trackedObj = trackedObjects[msg.object_key];

        if (trackedObj != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>Lost: {msg.object_key}</color>");
            }

            Destroy(trackedObj.gameObject);
            trackedObjects.Remove(msg.object_key);
            activeObjects--;
        }
    }

    /// <summary>
    /// Fügt Debug-Achsen hinzu (X=Rot, Y=Grün, Z=Blau)
    /// </summary>
    private void AddDebugAxes(GameObject obj)
    {
        // Erstelle drei LineRenderers für X, Y, Z Achsen
        float axisLength = 0.05f; // 5cm

        CreateAxisLine(obj, Vector3.right, Color.red, axisLength);    // X
        CreateAxisLine(obj, Vector3.up, Color.green, axisLength);     // Y
        CreateAxisLine(obj, Vector3.forward, Color.blue, axisLength); // Z
    }

    private void CreateAxisLine(GameObject parent, Vector3 direction, Color color, float length)
    {
        GameObject axisObj = new GameObject($"Axis_{direction}");
        axisObj.transform.SetParent(parent.transform, false);

        LineRenderer lr = axisObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.002f;
        lr.endWidth = 0.002f;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, direction * length);
    }

    /// <summary>
    /// Fügt Text-Label hinzu
    /// </summary>
    private void AddObjectLabel(GameObject obj, string labelText)
    {
        // TODO: Implementiere 3D Text oder TextMeshPro
        // Für jetzt: Marker via Gizmos in Scene View
    }

    /// <summary>
    /// Zeigt Stats im Inspector/GUI
    /// </summary>
    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 120, 300, 100));
            GUILayout.Label("<b>Object Manager</b>");
            GUILayout.Label($"Active Objects: {activeObjects}");
            GUILayout.Label($"Total Spawned: {totalObjectsSpawned}");
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Zeichne Gizmos in Scene View
    /// </summary>
    void OnDrawGizmos()
    {
        // Zeichne World Offset Position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(worldOffset, 0.05f);
    }
}