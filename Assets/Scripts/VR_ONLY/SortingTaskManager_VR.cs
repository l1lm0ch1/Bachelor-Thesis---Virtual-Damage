using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public string customCsvFolder = "";
    public string csvFileName = "sorting_vr_task_results.csv";

    [Header("Task Settings")]
    public float validationDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string userId = "Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Sorting_VR";

    private bool testRunning = false;
    private float testStartTime;
    private int correctPlacements = 0;
    private int incorrectPlacements = 0;

    private Dictionary<string, GameObject> cubes = new Dictionary<string, GameObject>();
    private Dictionary<string, bool> cubeValidated = new Dictionary<string, bool>();
    private Dictionary<string, string> cubeCorrectZone = new Dictionary<string, string>();

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
        if (cubeA1 != null) RegisterCube("A1", cubeA1, "A");
        if (cubeA2 != null) RegisterCube("A2", cubeA2, "A");
        if (cubeA3 != null) RegisterCube("A3", cubeA3, "A");
        if (cubeA4 != null) RegisterCube("A4", cubeA4, "A");

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

        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
            testType = AdminInfoPanel.Instance.GetTestType();
        }

        Debug.Log($"<color=cyan>SORTING TASK VR GESTARTET</color>");
        Debug.Log($"  User: {userId}");
        Debug.Log($"  Injury: {injuryLevel}");

        testRunning = true;
        testStartTime = Time.time;
        correctPlacements = 0;
        incorrectPlacements = 0;
        events.Clear();

        foreach (var kvp in cubeValidated.Keys.ToArray())
        {
            cubeValidated[kvp] = false;
        }

        if (feedbackManager != null)
        {
            feedbackManager.ResetAllCubes();
        }
    }

    public void OnCubePlacedInZone(string cubeKey, string cubeType, string zoneType, bool correct)
    {
        if (!testRunning)
            return;

        if (cubeValidated.ContainsKey(cubeKey) && cubeValidated[cubeKey])
        {
            if (showDebugLogs)
            {
                Debug.Log($"Cube {cubeKey} bereits validiert - ignoriere");
            }
            return;
        }

        float elapsedTime = Time.time - testStartTime;

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

            if (feedbackManager != null && cubes.ContainsKey(cubeKey))
            {
                feedbackManager.ShowIncorrectFeedback(cubes[cubeKey]);
            }
        }

        if (correctPlacements >= 8)
        {
            StartCoroutine(FinishTask());
        }
    }

    public void OnCubeLeftZone(string cubeKey, string zoneType)
    {
        // Optional: Handle cube removal
    }

    IEnumerator FinishTask()
    {
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
        CSVWriter writer = new CSVWriter(customCsvFolder, showDebugLogs);

        if (AdminInfoPanel.Instance != null)
        {
            AdminInfoPanel.Instance.SetCSVPath(writer.FolderPath);
        }

        List<(string, string, bool, float)> eventData = new List<(string, string, bool, float)>();
        foreach (var evt in events)
        {
            eventData.Add((evt.cubeId, evt.targetZone, evt.correct, evt.timestamp));
        }

        float totalTime = Time.time - testStartTime;

        writer.WriteSortingTestCSV(userId, injuryLevel, testType, totalTime, correctPlacements, incorrectPlacements, eventData, csvFileName);
    }

    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 340, 300, 300));
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
}

[Serializable]
public class SortingEvent
{
    public string cubeId;
    public string targetZone;
    public bool correct;
    public float timestamp;
}