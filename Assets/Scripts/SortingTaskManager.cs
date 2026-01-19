using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Sorting Task Manager - Collider-basierte Validation, Timer, CSV Export
/// PHYSICAL VERSION mit ArUco Cubes
/// </summary>
public class SortingTaskManager : MonoBehaviour
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
    public string customCsvFolder = "C:\\Users\\lilli\\OneDrive\\FH\\5. Semester\\BachelorArbeit\\ReactionTest_UserData\\TESTING LILLI";
    public string csvFileName = "sorting_task_results.csv";

    [Header("Task Settings")]
    [Tooltip("Wie lange muss Cube in Zone bleiben fuer Validation (Sekunden)")]
    public float validationDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // User Info (aus AdminInfoPanel)
    private string userId = "Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Sorting_Physical";

    // Task State
    private bool taskRunning = false;
    private float taskStartTime = 0f;

    // Counters
    private HashSet<string> validatedCubes = new HashSet<string>();
    private int totalPlacements = 0;
    private int correctPlacements = 0;
    private int incorrectPlacements = 0;

    // Event Log
    private List<PlacementEvent> placementEvents = new List<PlacementEvent>();

    // Cube Mapping
    private Dictionary<string, GameObject> cubeObjects = new Dictionary<string, GameObject>();

    // Validation Coroutines
    private Dictionary<string, Coroutine> validationCoroutines = new Dictionary<string, Coroutine>();

    // CSV Paths
    private string csvPath;
    private string csvEventsPath;

    void Start()
    {
        // Feedback Manager finden
        if (feedbackManager == null)
        {
            feedbackManager = FindFirstObjectByType<VisualFeedbackManager>();
        }

        // Cube Mapping aufbauen
        cubeObjects["A1"] = cubeA1;
        cubeObjects["A2"] = cubeA2;
        cubeObjects["A3"] = cubeA3;
        cubeObjects["A4"] = cubeA4;
        cubeObjects["B1"] = cubeB1;
        cubeObjects["B2"] = cubeB2;
        cubeObjects["B3"] = cubeB3;
        cubeObjects["B4"] = cubeB4;

        // CSV Paths
        string csvFilename = csvFileName; // Default Fallback
        string csvEventsFilename = "sorting_task_events.csv";

        if (AdminInfoPanel.Instance != null)
        {
            csvFilename = AdminInfoPanel.Instance.GetSortingTaskCSVFilename();
            csvEventsFilename = csvFilename.Replace(".csv", "_events.csv");
        }

        if (string.IsNullOrEmpty(customCsvFolder))
        {
            csvPath = Path.Combine(Application.persistentDataPath, csvFilename);
            csvEventsPath = Path.Combine(Application.persistentDataPath, csvEventsFilename);
        }
        else
        {
            csvPath = Path.Combine(customCsvFolder, csvFilename);
            csvEventsPath = Path.Combine(customCsvFolder, csvEventsFilename);

            if (!Directory.Exists(customCsvFolder))
            {
                try
                {
                    Directory.CreateDirectory(customCsvFolder);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Kann CSV Ordner nicht erstellen: {e.Message}");
                    csvPath = Path.Combine(Application.persistentDataPath, csvFilename);
                    csvEventsPath = Path.Combine(Application.persistentDataPath, csvEventsFilename);
                }
            }
        }

        Debug.Log("<color=green>Sorting Task Manager initialisiert (COLLIDER MODE)</color>");
        Debug.Log($"  CSV Path: {csvPath}");
        Debug.Log($"  Events CSV: {csvEventsPath}");
    }

    /// <summary>
    /// Startet Sorting Task
    /// </summary>
    public void StartSortingTask()
    {
        if (taskRunning)
        {
            Debug.LogWarning("Task laeuft bereits!");
            return;
        }

        // User Info aus AdminInfoPanel holen
        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
            testType = AdminInfoPanel.Instance.GetTestType();
        }
        else
        {
            Debug.LogWarning("AdminInfoPanel nicht gefunden - nutze Default Werte");
        }

        Debug.Log("<color=cyan>SORTING TASK GESTARTET (COLLIDER)</color>");
        Debug.Log($"  User: {userId}");
        Debug.Log($"  Injury: {injuryLevel}");
        Debug.Log($"  Test Type: {testType}");

        taskRunning = true;
        taskStartTime = Time.time;

        // Reset
        validatedCubes.Clear();
        placementEvents.Clear();
        totalPlacements = 0;
        correctPlacements = 0;
        incorrectPlacements = 0;
        validationCoroutines.Clear();

        // Reset Visual Feedback
        if (feedbackManager != null)
        {
            feedbackManager.ResetAllCubes();
        }
    }

    /// <summary>
    /// Stoppt Sorting Task manuell
    /// </summary>
    public void StopSortingTask()
    {
        if (!taskRunning)
            return;

        taskRunning = false;
        float totalTime = Time.time - taskStartTime;

        Debug.Log("<color=green>SORTING TASK BEENDET</color>");
        Debug.Log($"Zeit: {totalTime:F1}s");
        Debug.Log($"Validiert: {validatedCubes.Count}/8 Wuerfel");
        Debug.Log($"Korrekt: {correctPlacements}, Falsch: {incorrectPlacements}");

        ExportToCSV(totalTime, "Manual");
    }

    /// <summary>
    /// Cube wurde in Zone platziert (von ZoneValidator aufgerufen)
    /// </summary>
    public void OnCubePlacedInZone(string cubeKey, string cubeType, string zoneName, bool correct)
    {
        if (!taskRunning)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("Cube platziert aber Task nicht aktiv!");
            }
            return;
        }

        // Cube schon validiert?
        if (validatedCubes.Contains(cubeKey))
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>{cubeKey} bereits validiert</color>");
            }
            return;
        }

        // Starte Validation Delay
        if (validationCoroutines.ContainsKey(cubeKey))
        {
            StopCoroutine(validationCoroutines[cubeKey]);
        }

        validationCoroutines[cubeKey] = StartCoroutine(ValidateCubePlacement(cubeKey, cubeType, zoneName, correct));
    }

    /// <summary>
    /// Cube hat Zone verlassen (von ZoneValidator aufgerufen)
    /// </summary>
    public void OnCubeLeftZone(string cubeKey, string zoneName)
    {
        // Abbruch Validation wenn Cube Zone verlaesst
        if (validationCoroutines.ContainsKey(cubeKey))
        {
            StopCoroutine(validationCoroutines[cubeKey]);
            validationCoroutines.Remove(cubeKey);

            if (showDebugLogs)
            {
                Debug.Log($"<color=orange>{cubeKey} Zone verlassen - Validation abgebrochen</color>");
            }
        }
    }

    /// <summary>
    /// Coroutine: Wartet Delay ab bevor Validation
    /// </summary>
    private IEnumerator ValidateCubePlacement(string cubeKey, string cubeType, string zoneName, bool correct)
    {
        // Warte kurz (User soll Cube loslassen)
        yield return new WaitForSeconds(validationDelay);

        // Zeit seit Start
        float timeSinceStart = Time.time - taskStartTime;

        // Event loggen
        placementEvents.Add(new PlacementEvent
        {
            cubeKey = cubeKey,
            cubeType = cubeType,
            zoneName = zoneName,
            correct = correct,
            timeSinceStart = timeSinceStart
        });

        totalPlacements++;

        if (correct)
        {
            correctPlacements++;

            // Als validiert markieren
            validatedCubes.Add(cubeKey);

            // Visual Feedback
            if (feedbackManager != null && cubeObjects.ContainsKey(cubeKey))
            {
                feedbackManager.ShowCorrectFeedback(cubeObjects[cubeKey]);
            }

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>{cubeKey} KORREKT validiert ({validatedCubes.Count}/8)</color>");
            }

            // Check ob fertig
            if (validatedCubes.Count >= 8)
            {
                float totalTime = Time.time - taskStartTime;
                taskRunning = false;

                Debug.Log("<color=green>SORTING TASK KOMPLETT!</color>");
                Debug.Log($"Zeit: {totalTime:F1}s");
                Debug.Log($"Fehler: {incorrectPlacements}");

                ExportToCSV(totalTime, "Complete");
            }
        }
        else
        {
            incorrectPlacements++;

            // Visual Feedback (rot blinken)
            if (feedbackManager != null && cubeObjects.ContainsKey(cubeKey))
            {
                feedbackManager.ShowIncorrectFeedback(cubeObjects[cubeKey]);
            }

            if (showDebugLogs)
            {
                Debug.Log($"<color=red>{cubeKey} FALSCH (Typ {cubeType} in Zone {zoneName})</color>");
            }
        }

        // Remove from tracking
        validationCoroutines.Remove(cubeKey);
    }

    /// <summary>
    /// CSV Export
    /// </summary>
    private void ExportToCSV(float totalTime, string completionStatus)
    {
        try
        {
            // Summary CSV
            bool fileExists = File.Exists(csvPath);

            using (StreamWriter writer = new StreamWriter(csvPath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Total_Time_sec,Total_Placements,Correct_Placements,Incorrect_Placements,Unique_Cubes_Validated,Completion");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string line = $"{timestamp},{userId},{injuryLevel},{testType},{totalTime:F2},{totalPlacements},{correctPlacements},{incorrectPlacements},{validatedCubes.Count},{completionStatus}";
                writer.WriteLine(line);
            }

            Debug.Log($"<color=green>Sorting Results gespeichert: {csvPath}</color>");

            // Events CSV
            fileExists = File.Exists(csvEventsPath);

            using (StreamWriter writer = new StreamWriter(csvEventsPath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Event_Nr,Cube_Key,Cube_Type,Zone,Correct,Time_Since_Start_sec");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                for (int i = 0; i < placementEvents.Count; i++)
                {
                    var evt = placementEvents[i];
                    string line = $"{timestamp},{userId},{injuryLevel},{testType},{i + 1},{evt.cubeKey},{evt.cubeType},{evt.zoneName},{evt.correct},{evt.timeSinceStart:F2}";
                    writer.WriteLine(line);
                }
            }

            Debug.Log($"<color=green>Sorting Events gespeichert: {csvEventsPath}</color>");

        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim CSV Export: {e.Message}");
        }
    }

    /// <summary>
    /// OnGUI - Debug UI
    /// </summary>
    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 480, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>SORTING TASK (Physical)</b>");
            GUILayout.Space(5);

            // User Info aus AdminInfoPanel anzeigen
            if (AdminInfoPanel.Instance != null)
            {
                GUILayout.Label($"User: {AdminInfoPanel.Instance.GetUserName()}");
                GUILayout.Label($"Injury: {AdminInfoPanel.Instance.GetInjuryState()}");
                GUILayout.Space(5);
            }

            if (!taskRunning)
            {
                if (GUILayout.Button("START SORTING TASK", GUILayout.Height(40)))
                {
                    StartSortingTask();
                }
            }
            else
            {
                GUILayout.Label($"Validiert: {validatedCubes.Count}/8");
                GUILayout.Label($"Korrekt: {correctPlacements}");
                GUILayout.Label($"Fehler: {incorrectPlacements}");

                float elapsed = Time.time - taskStartTime;
                GUILayout.Label($"Zeit: {elapsed:F1}s");

                if (GUILayout.Button("STOP (manuell)"))
                {
                    StopSortingTask();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Cubes in Zonen:</b>");
            ZoneValidator[] zones = FindObjectsByType<ZoneValidator>(FindObjectsSortMode.None);
            foreach (var zone in zones)
            {
                GUILayout.Label($"Zone {zone.zoneType}: {zone.GetCubeCount()}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// CSV Ordner oeffnen
    /// </summary>
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
/// Placement Event Data
/// </summary>
[Serializable]
public class PlacementEvent
{
    public string cubeKey;
    public string cubeType;
    public string zoneName;
    public bool correct;
    public float timeSinceStart;
}