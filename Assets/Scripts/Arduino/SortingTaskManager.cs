using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
    public string customCsvFolder = "";
    public string csvFileName = "sorting_task_results.csv";

    [Header("Task Settings")]
    public float validationDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string userId = "Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Sorting_Physical";

    private bool taskRunning = false;
    private float taskStartTime = 0f;

    private HashSet<string> validatedCubes = new HashSet<string>();
    private int totalPlacements = 0;
    private int correctPlacements = 0;
    private int incorrectPlacements = 0;

    private List<PlacementEvent> placementEvents = new List<PlacementEvent>();

    private Dictionary<string, GameObject> cubeObjects = new Dictionary<string, GameObject>();

    private Dictionary<string, Coroutine> validationCoroutines = new Dictionary<string, Coroutine>();

    void Start()
    {
        if (feedbackManager == null)
        {
            feedbackManager = FindFirstObjectByType<VisualFeedbackManager>();
        }

        cubeObjects["A1"] = cubeA1;
        cubeObjects["A2"] = cubeA2;
        cubeObjects["A3"] = cubeA3;
        cubeObjects["A4"] = cubeA4;
        cubeObjects["B1"] = cubeB1;
        cubeObjects["B2"] = cubeB2;
        cubeObjects["B3"] = cubeB3;
        cubeObjects["B4"] = cubeB4;

        Debug.Log("<color=green>Sorting Task Manager initialisiert (COLLIDER MODE)</color>");
    }

    public void StartSortingTask()
    {
        if (taskRunning)
        {
            Debug.LogWarning("Task laeuft bereits!");
            return;
        }

        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
            testType = AdminInfoPanel.Instance.GetTestType();
        }

        Debug.Log("<color=cyan>SORTING TASK GESTARTET (COLLIDER)</color>");
        Debug.Log($"  User: {userId}");
        Debug.Log($"  Injury: {injuryLevel}");

        taskRunning = true;
        taskStartTime = Time.time;

        validatedCubes.Clear();
        placementEvents.Clear();
        totalPlacements = 0;
        correctPlacements = 0;
        incorrectPlacements = 0;
        validationCoroutines.Clear();

        if (feedbackManager != null)
        {
            feedbackManager.ResetAllCubes();
        }
    }

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

        ExportToCSV(totalTime);
    }

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

        if (validatedCubes.Contains(cubeKey))
        {
            if (showDebugLogs)
            {
                Debug.Log($"<color=yellow>{cubeKey} bereits validiert</color>");
            }
            return;
        }

        if (validationCoroutines.ContainsKey(cubeKey))
        {
            StopCoroutine(validationCoroutines[cubeKey]);
        }

        validationCoroutines[cubeKey] = StartCoroutine(ValidateCubePlacement(cubeKey, cubeType, zoneName, correct));
    }

    public void OnCubeLeftZone(string cubeKey, string zoneName)
    {
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

    private IEnumerator ValidateCubePlacement(string cubeKey, string cubeType, string zoneName, bool correct)
    {
        yield return new WaitForSeconds(validationDelay);

        float timeSinceStart = Time.time - taskStartTime;

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

            validatedCubes.Add(cubeKey);

            if (feedbackManager != null && cubeObjects.ContainsKey(cubeKey))
            {
                feedbackManager.ShowCorrectFeedback(cubeObjects[cubeKey]);
            }

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>{cubeKey} KORREKT validiert ({validatedCubes.Count}/8)</color>");
            }

            if (validatedCubes.Count >= 8)
            {
                float totalTime = Time.time - taskStartTime;
                taskRunning = false;

                Debug.Log("<color=green>SORTING TASK KOMPLETT!</color>");
                Debug.Log($"Zeit: {totalTime:F1}s");
                Debug.Log($"Fehler: {incorrectPlacements}");

                ExportToCSV(totalTime);
            }
        }
        else
        {
            incorrectPlacements++;

            if (feedbackManager != null && cubeObjects.ContainsKey(cubeKey))
            {
                feedbackManager.ShowIncorrectFeedback(cubeObjects[cubeKey]);
            }

            if (showDebugLogs)
            {
                Debug.Log($"<color=red>{cubeKey} FALSCH (Typ {cubeType} in Zone {zoneName})</color>");
            }
        }

        validationCoroutines.Remove(cubeKey);
    }

    private void ExportToCSV(float totalTime)
    {
        CSVWriter writer = new CSVWriter(customCsvFolder, showDebugLogs);

        if (AdminInfoPanel.Instance != null)
        {
            AdminInfoPanel.Instance.SetCSVPath(writer.FolderPath);
        }

        List<(string, string, bool, float)> eventData = new List<(string, string, bool, float)>();
        foreach (var evt in placementEvents)
        {
            eventData.Add((evt.cubeKey, evt.cubeType, evt.correct, evt.timeSinceStart));
        }

        writer.WriteSortingTestCSV(userId, injuryLevel, testType, totalTime, correctPlacements, incorrectPlacements, eventData, csvFileName);
    }

    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 480, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>SORTING TASK (Physical)</b>");
            GUILayout.Space(5);

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

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}

[Serializable]
public class PlacementEvent
{
    public string cubeKey;
    public string cubeType;
    public string zoneName;
    public bool correct;
    public float timeSinceStart;
}