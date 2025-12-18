using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zone Validator - Erkennt Cubes die in Zone kommen via Trigger
/// Attached auf Zone GameObject mit BoxCollider (Is Trigger)
/// </summary>
public class ZoneValidator : MonoBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("Zone Typ: A oder B")]
    public string zoneType = "A";

    [Header("Visualization")]
    [Tooltip("Zeige Zone als farbige Box")]
    public bool showZoneVisual = true;

    [Tooltip("Zone Farbe (Gizmos)")]
    public Color zoneColor = Color.green;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Cubes die aktuell in Zone sind
    private HashSet<string> cubesInZone = new HashSet<string>();

    // Reference zu Manager
    private SortingTaskManager sortingManager;

    void Start()
    {
        // BoxCollider Check
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError($"Zone {zoneType}: Kein BoxCollider gefunden!");
            return;
        }

        if (!boxCollider.isTrigger)
        {
            Debug.LogWarning($"Zone {zoneType}: BoxCollider ist kein Trigger! Setze Is Trigger = true");
            boxCollider.isTrigger = true;
        }

        // Finde SortingTaskManager
        sortingManager = FindFirstObjectByType<SortingTaskManager>();

        if (sortingManager == null)
        {
            Debug.LogWarning($"Zone {zoneType}: SortingTaskManager nicht gefunden!");
        }

        Debug.Log($"<color=cyan>Zone {zoneType} initialisiert</color>");
    }

    /// <summary>
    /// Cube betritt Zone
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Prüfe ob es ein Cube ist
        TrackedObject cube = other.GetComponent<TrackedObject>();

        if (cube == null)
            return;

        // Prüfe ob Cube schon in Zone ist (Mehrfach-Trigger vermeiden)
        if (cubesInZone.Contains(cube.objectKey))
            return;

        // Füge zu Liste hinzu
        cubesInZone.Add(cube.objectKey);

        // Prüfe ob korrekte Zone
        bool correct = (cube.objectType == zoneType);

        if (showDebugLogs)
        {
            string status = correct ? "KORREKT" : "FALSCH";
            Debug.Log($"<color=yellow>Zone {zoneType}: {cube.objectKey} ({cube.objectType}) -> {status}</color>");
        }

        // Benachrichtige Manager
        if (sortingManager != null)
        {
            sortingManager.OnCubePlacedInZone(cube.objectKey, cube.objectType, zoneType, correct);
        }
    }

    /// <summary>
    /// Cube verlässt Zone
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        TrackedObject cube = other.GetComponent<TrackedObject>();

        if (cube == null)
            return;

        // Entferne aus Liste
        if (cubesInZone.Contains(cube.objectKey))
        {
            cubesInZone.Remove(cube.objectKey);

            if (showDebugLogs)
            {
                Debug.Log($"<color=gray>Zone {zoneType}: {cube.objectKey} verlassen</color>");
            }

            // Optional: Benachrichtige Manager
            if (sortingManager != null)
            {
                sortingManager.OnCubeLeftZone(cube.objectKey, zoneType);
            }
        }
    }

    /// <summary>
    /// Gibt Anzahl Cubes in Zone zurück
    /// </summary>
    public int GetCubeCount()
    {
        return cubesInZone.Count;
    }

    /// <summary>
    /// Prüft ob bestimmter Cube in Zone ist
    /// </summary>
    public bool ContainsCube(string cubeKey)
    {
        return cubesInZone.Contains(cubeKey);
    }

    /// <summary>
    /// Reset Zone
    /// </summary>
    public void ClearZone()
    {
        cubesInZone.Clear();
    }

    /// <summary>
    /// Zeichne Zone in Scene View
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showZoneVisual)
            return;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            return;

        // Zeichne Wireframe Box
        Gizmos.color = zoneColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

        // Transparente Box
        Color fillColor = zoneColor;
        fillColor.a = 0.1f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(boxCollider.center, boxCollider.size);
    }

    /// <summary>
    /// Zeichne Zone Label in Scene View
    /// </summary>
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Vector3 labelPos = transform.position + Vector3.up * (boxCollider.size.y / 2 + 0.05f);
            UnityEditor.Handles.Label(labelPos, $"Zone {zoneType}\n{cubesInZone.Count} Cubes");
        }
#endif
    }
}