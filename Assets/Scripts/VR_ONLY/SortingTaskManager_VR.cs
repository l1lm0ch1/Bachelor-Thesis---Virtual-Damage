using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// SORTING TASK MANAGER VR - NICHT ZEIT-BASIERT
/// Orientiert am originalen SortingTaskManager (Physical)
/// Task endet wenn alle Objekte korrekt sortiert sind
/// OHNE eigene ZoneValidator Klasse (nutzt separate ZoneValidator.cs)
/// </summary>
public class SortingTaskManager_VR : MonoBehaviour
{
    [Header("References")]
    public VisualFeedbackManager feedbackManager;

    [Header("Virtual Cubes (8 GameObjects - A1-A4, B1-B4)")]
    public GameObject cubeA1;
    public GameObject cubeA2;
    public GameObject cubeA3;
    public GameObject cubeA4;
    public GameObject cubeB1;
    public GameObject cubeB2;
    public GameObject cubeB3;
    public GameObject cubeB4;

    [Header("CSV Export")]
    [Tooltip("Leer lassen fuer Default Path")]
    public string customCsvFolder = "";
    public string csvFileName = "sorting_vr_task_results.csv";

    [Header("Task Settings")]
    [Tooltip("Wie lange muss Cube in Zone bleiben fuer Validation (Sekunden)")]
    public float validationDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // User Info (aus AdminInfoPanel)
    private string userId = "Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Sorting_VR";

    // Test State - NICHT zeit-basiert
    private bool testRunning = false;
    private float testStartTime;
    private int correctPlacements = 0;
    private int incorrectPlacements = 0;

    // Cube tracking
    private Dictionary<string, GameObject> cubes = new Dictionary<string, GameObject>();
    private Dictionary<string, bool> cubeValidated = new Dictionary<string, bool>();
    private Dictionary<string, string> cubeCorrectZone = new Dictionary<string, string>();

    // Events fuer CSV
    private List<SortingEvent> events = new List<SortingEvent>();

    void Start()
    {
        SetupCubes();

        if (showDebugLogs)
        {
            Debug.Log("<color=cyan>SortingTaskManager_VR initialisiert (NICHT zeit-basiert)</color>");
        }
    }

    void SetupCubes()
    {
        // A-Cubes
        if (cubeA1 != null) RegisterCube("A1", cubeA1, "A");
        if (cubeA2 != null) RegisterCube("A2", cubeA2, "A");
        if (cubeA3 != null) RegisterCube("A3", cubeA3, "A");
        if (cubeA4 != null) RegisterCube("A4", cubeA4, "A");

        // B-Cubes
        if (cubeB1 != null) RegisterCube("B1", cubeB1, "B");
        if (cubeB2 != null) RegisterCube("B2", cubeB2, "B");
        if (cubeB3 != null) RegisterCube("B3", cubeB3, "B");
        if (cubeB4 != null) RegisterCube("B4", cubeB4, "B");
    }

    void RegisterCube(string cubeId, GameObject cubeObj, string correctZone)
    {
        cubes[cubeId] = cubeObj;
        cubeValidated[cubeId] = false;
        cubeCorrectZone[cubeId] = correctZone;
    }

    public void StartSortingTask()
    {
        if (testRunning)
        {
            Debug.LogWarning("Sorting Task laeuft bereits!");
            return;
        }

        // User Info aus AdminInfoPanel holen
        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
        }
        else
        {
            Debug.LogWarning("AdminInfoPanel nicht gefunden - nutze Default Werte");
        }

        Debug.Log($"<color=cyan>SORTING TASK VR GESTARTET</color>");
        Debug.Log($"  User: {userId}");
        Debug.Log($"  Injury: {injuryLevel}");
        Debug.Log($"  Test Type: {testType}");

        testRunning = true;
        testStartTime = Time.time;
        correctPlacements = 0;
        incorrectPlacements = 0;
        events.Clear();

        // Reset alle Cubes
        foreach (var kvp in cubeValidated.Keys.ToArray())
        {
            cubeValidated[kvp] = false;
        }

        if (feedbackManager != null)
        {
            feedbackManager.ResetAllCubes();
        }
    }

    /// <summary>
    /// Callback von ZoneValidator wenn Cube platziert wird
    /// Diese Methode wird von deiner separaten ZoneValidator.cs aufgerufen
    /// </summary>
    public void OnCubePlacedInZone(string cubeKey, string cubeType, string zoneType, bool correct)
    {
        if (!testRunning)
            return;

        // Verhindere doppelte Validierung
        if (cubeValidated.ContainsKey(cubeKey) && cubeValidated[cubeKey])
        {
            if (showDebugLogs)
            {
                Debug.Log($"Cube {cubeKey} bereits validiert - ignoriere");
            }
            return;
        }

        float elapsedTime = Time.time - testStartTime;

        // Event speichern
        events.Add(new SortingEvent
        {
            cubeId = cubeKey,
            targetZone = zoneType,
            correct = correct,
            timestamp = elapsedTime
        });

        if (correct)
        {
            correctPlacements++;
            cubeValidated[cubeKey] = true;

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>KORREKT: {cubeKey} auf Zone {zoneType} ({correctPlacements}/8)</color>");
            }

            // Visuelles Feedback
            if (feedbackManager != null && cubes.ContainsKey(cubeKey))
            {
                feedbackManager.ShowCorrectFeedback(cubes[cubeKey]);
            }
        }
        else
        {
            incorrectPlacements++;

            if (showDebugLogs)
            {
                Debug.Log($"<color=red>FALSCH: {cubeKey} auf Zone {zoneType}</color>");
            }

            // Visuelles Feedback
            if (feedbackManager != null && cubes.ContainsKey(cubeKey))
            {
                feedbackManager.ShowIncorrectFeedback(cubes[cubeKey]);
            }
        }

        // Check ob Task fertig (alle 8 Cubes korrekt platziert)
        if (correctPlacements >= 8)
        {
            StartCoroutine(FinishTask());
        }
    }

    /// <summary>
    /// Optional: Callback wenn Cube Zone verlaesst
    /// </summary>
    public void OnCubeLeftZone(string cubeKey, string zoneType)
    {
        // Optional: Handle cube removal
        // Aktuell wird nichts gemacht wenn Cube Zone verlaesst
    }

    IEnumerator FinishTask()
    {
        // Kurz warten damit letzte Feedback-Effekte sichtbar sind
        yield return new WaitForSeconds(0.5f);

        float totalTime = Time.time - testStartTime;

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>SORTING TASK ABGESCHLOSSEN!</color>");
            Debug.Log($"  Zeit: {totalTime:F2}s");
            Debug.Log($"  Korrekt: {correctPlacements}");
            Debug.Log($"  Falsch: {incorrectPlacements}");
        }

        testRunning = false;
        ExportToCSV();
    }

    void ExportToCSV()
    {
        string folderPath = string.IsNullOrEmpty(customCsvFolder)
            ? Application.persistentDataPath
            : customCsvFolder;

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Summary CSV
        string summaryPath = Path.Combine(folderPath, csvFileName);
        bool fileExists = File.Exists(summaryPath);

        using (StreamWriter writer = new StreamWriter(summaryPath, true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Total_Time_sec,Correct_Placements,Incorrect_Placements,Total_Placements,Accuracy_Percent");
            }

            float totalTime = Time.time - testStartTime;
            int totalPlacements = correctPlacements + incorrectPlacements;
            float accuracy = totalPlacements > 0 ? (correctPlacements / (float)totalPlacements) * 100f : 0f;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"{timestamp},{userId},{injuryLevel},{testType},{totalTime:F2},{correctPlacements},{incorrectPlacements},{totalPlacements},{accuracy:F2}";

            writer.WriteLine(line);
        }

        // Events CSV
        string eventsFileName = csvFileName.Replace(".csv", "_events.csv");
        string eventsPath = Path.Combine(folderPath, eventsFileName);
        bool eventsFileExists = File.Exists(eventsPath);

        using (StreamWriter writer = new StreamWriter(eventsPath, true))
        {
            if (!eventsFileExists)
            {
                writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Event_Nr,Cube_ID,Target_Zone,Correct,Time_Since_Start_sec");
            }

            int eventNr = 1;
            foreach (var evt in events)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string line = $"{timestamp},{userId},{injuryLevel},{testType},{eventNr},{evt.cubeId},{evt.targetZone},{evt.correct},{evt.timestamp:F3}";
                writer.WriteLine(line);
                eventNr++;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>CSV gespeichert:</color>\n  {summaryPath}\n  {eventsPath}");
        }
    }

    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 20));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Sorting Task VR (NICHT zeit-basiert)</b>");
            GUILayout.Space(10);

            if (AdminInfoPanel.Instance != null)
            {
                GUILayout.Label($"User: {AdminInfoPanel.Instance.GetUserName()}");
                GUILayout.Label($"Injury: {AdminInfoPanel.Instance.GetInjuryState()}");
                GUILayout.Space(10);
            }

            if (!testRunning)
            {
                if (GUILayout.Button("START SORTING TASK", GUILayout.Height(40)))
                {
                    StartSortingTask();
                }
            }
            else
            {
                float elapsedTime = Time.time - testStartTime;
                GUILayout.Label($"Zeit: {elapsedTime:F1}s");
                GUILayout.Label($"Korrekt: {correctPlacements}/8");
                GUILayout.Label($"Falsch: {incorrectPlacements}");

                GUILayout.Space(10);
                GUILayout.Label("Task endet wenn 8/8 korrekt platziert");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    [ContextMenu("CSV Ordner oeffnen")]
    public void OpenCsvFolder()
    {
        string folderPath = string.IsNullOrEmpty(customCsvFolder)
            ? Application.persistentDataPath
            : customCsvFolder;

        if (Directory.Exists(folderPath))
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
#endif
            Debug.Log($"CSV Ordner geoeffnet: {folderPath}");
        }
        else
        {
            Debug.LogWarning($"Ordner existiert nicht: {folderPath}");
        }
    }
}

/// <summary>
/// Sorting Event Data
/// </summary>
[Serializable]
public class SortingEvent
{
    public string cubeId;
    public string targetZone;
    public bool correct;
    public float timestamp;
}