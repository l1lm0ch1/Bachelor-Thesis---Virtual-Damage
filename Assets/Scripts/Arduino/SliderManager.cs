using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Slider Manager - Bis zum Anschlag bewegen
/// User muss Slider ganz links oder ganz rechts bewegen
/// </summary>
public class SliderManager : MonoBehaviour
{
    [Header("References")]
    public ArduinoUDPReceiver arduinoReceiver;

    [Header("Virtual Sliders (2 GameObjects in VR)")]
    public GameObject slider1Object;
    public GameObject slider2Object;

    [Header("Slider 1 Unity Positions")]
    [Tooltip("Position when Slider 1 is at 0.0 (left/base position)")]
    public Vector3 slider1MinUnityPosition = new Vector3(0, 0.2f, 0);

    [Tooltip("Position when Slider 1 is at 1.0 (right/full extension)")]
    public Vector3 slider1MaxUnityPosition = new Vector3(0.2f, 0.2f, 0);

    [Header("Slider 2 Unity Positions")]
    [Tooltip("Position when Slider 2 is at 0.0 (left/base position)")]
    public Vector3 slider2MinUnityPosition = new Vector3(0, 0.2f, 0);

    [Tooltip("Position when Slider 2 is at 1.0 (right/full extension)")]
    public Vector3 slider2MaxUnityPosition = new Vector3(0.2f, 0.2f, 0);

    [Header("Test Settings")]
    [Tooltip("Test Dauer in Sekunden")]
    public float testDuration = 60f;

    [Tooltip("Delay zwischen Tasks (Sekunden)")]
    public float delayBetweenTasks = 1.5f;

    [Tooltip("Tolerance (wie nah am Anschlag)")]
    [Range(0.01f, 0.2f)]
    public float endPositionTolerance = 0.1f;

    [Header("CSV Export")]
    [Tooltip("Leer lassen fuer Default Path")]
    public string customCsvFolder = "";
    public string csvFileName = "slider_test_results.csv";
    public string userId = "User_01";
    public int injuryLevel = 0;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Test State
    private bool testRunning = false;
    private float testStartTime = 0f;
    private float testElapsedTime = 0f;
    private int tasksCompleted = 0;

    // Current Task
    private int currentSlider = 0;
    private bool targetIsMax = false;
    private float taskStartTime = 0f;
    private bool waitingForTask = false;
    private float taskStartValue = 0f;

    // Current Slider Values
    private float slider1Value = 0f;
    private float slider2Value = 0f;

    // Results
    private List<SliderTaskResult> results = new List<SliderTaskResult>();

    // CSV Path
    private string csvPath;

    void Start()
    {
        if (arduinoReceiver == null)
        {
            arduinoReceiver = FindFirstObjectByType<ArduinoUDPReceiver>();
        }

        if (arduinoReceiver == null)
        {
            Debug.LogError("ArduinoUDPReceiver nicht gefunden!");
            return;
        }

        arduinoReceiver.SliderChanged += OnSliderChanged;

        if (string.IsNullOrEmpty(customCsvFolder))
        {
            csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
        }
        else
        {
            csvPath = Path.Combine(customCsvFolder, csvFileName);

            if (!Directory.Exists(customCsvFolder))
            {
                try
                {
                    Directory.CreateDirectory(customCsvFolder);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Kann CSV Ordner nicht erstellen: {e.Message}");
                    csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
                }
            }
        }

        Debug.Log("<color=green>Slider Manager initialisiert (BIS ZUM ANSCHLAG)</color>");
        Debug.Log($"  CSV Path: {csvPath}");
        Debug.Log($"  Test Duration: {testDuration}s");
        Debug.Log($"  Slider 1 Unity Pos: {slider1MinUnityPosition} -> {slider1MaxUnityPosition}");
        Debug.Log($"  Slider 2 Unity Pos: {slider2MinUnityPosition} -> {slider2MaxUnityPosition}");
    }

    void OnDestroy()
    {
        if (arduinoReceiver != null)
        {
            arduinoReceiver.SliderChanged -= OnSliderChanged;
        }
    }

    void Update()
    {
        // PREVIEW MODE: IMMER Slider Position updaten (auch ohne Test)
        if (slider1Object != null)
        {
            UpdateSliderVisual(slider1Object, slider1Value, slider1MinUnityPosition, slider1MaxUnityPosition);
        }

        if (slider2Object != null)
        {
            UpdateSliderVisual(slider2Object, slider2Value, slider2MinUnityPosition, slider2MaxUnityPosition);
        }

        // TEST MODE: Timer nur waehrend Test
        if (testRunning)
        {
            testElapsedTime = Time.time - testStartTime;
        }
    }

    public void StartSliderTest()
    {
        if (testRunning)
        {
            Debug.LogWarning("Test laeuft bereits!");
            return;
        }

        Debug.Log($"<color=cyan>SLIDER TEST GESTARTET ({testDuration}s)</color>");

        testRunning = true;
        testStartTime = Time.time;
        testElapsedTime = 0f;
        tasksCompleted = 0;
        results.Clear();

        StartCoroutine(RunTest());
    }

    public void StopSliderTest()
    {
        if (!testRunning)
            return;

        StopAllCoroutines();
        testRunning = false;
        waitingForTask = false;

        Debug.Log("<color=yellow>SLIDER TEST MANUELL BEENDET</color>");

        ResetSliderHighlights();
        ExportToCSV();
    }

    private IEnumerator RunTest()
    {
        while (testElapsedTime < testDuration)
        {
            float timeRemaining = testDuration - testElapsedTime;
            if (timeRemaining < 5f)
            {
                break;
            }

            tasksCompleted++;
            currentSlider = UnityEngine.Random.Range(1, 3);

            // Bestimme Richtung basierend auf aktueller Position
            taskStartValue = currentSlider == 1 ? slider1Value : slider2Value;

            // Wenn Slider nah an 0.0 (links) -> bewege nach rechts (1.0)
            // Wenn Slider nah an 1.0 (rechts) -> bewege nach links (0.0)
            targetIsMax = taskStartValue < 0.5f;

            string direction = targetIsMax ? "RECHTS" : "LINKS";

            if (showDebugLogs)
            {
                Debug.Log($"Task {tasksCompleted}: Slider {currentSlider} nach {direction} (Von: {taskStartValue:F2}, Zeit: {testElapsedTime:F1}s)");
            }

            HighlightSlider(currentSlider, targetIsMax);

            taskStartTime = Time.time;
            waitingForTask = true;

            while (waitingForTask)
            {
                testElapsedTime = Time.time - testStartTime;

                if (testElapsedTime >= testDuration)
                {
                    waitingForTask = false;
                    break;
                }

                yield return null;
            }

            ResetSliderHighlights();

            testElapsedTime = Time.time - testStartTime;

            if (testElapsedTime + delayBetweenTasks < testDuration)
            {
                yield return new WaitForSeconds(delayBetweenTasks);
            }
            else
            {
                break;
            }

            testElapsedTime = Time.time - testStartTime;
        }

        testRunning = false;
        float finalTime = Time.time - testStartTime;

        Debug.Log("<color=green>SLIDER TEST ABGESCHLOSSEN</color>");
        Debug.Log($"  Dauer: {finalTime:F1}s");
        Debug.Log($"  Tasks: {tasksCompleted}");

        ExportToCSV();
    }

    private void OnSliderChanged(int sliderId, float value)
    {
        // PREVIEW MODE: Wert IMMER updaten (auch ohne Test)
        if (sliderId == 1)
        {
            slider1Value = value;
        }
        else if (sliderId == 2)
        {
            slider2Value = value;
        }

        // Optional: Preview Mode Debug
        if (showDebugLogs && !waitingForTask)
        {
            // Zeige nur bei groesseren Aenderungen (alle 10%)
            int percent = Mathf.RoundToInt(value * 10) * 10;
            if (percent % 20 == 0)  // Bei 0%, 20%, 40%, 60%, 80%, 100%
            {
                Debug.Log($"[PREVIEW] Slider {sliderId}: {value:F2} ({percent}%)");
            }
        }

        // TEST MODE: Task Check nur waehrend Test
        if (!waitingForTask || sliderId != currentSlider)
            return;

        // Ziel ist entweder 0.0 (links) oder 1.0 (rechts)
        float targetPosition = targetIsMax ? 1.0f : 0.0f;

        // Check ob Anschlag erreicht
        float distance = Mathf.Abs(value - targetPosition);

        if (distance <= endPositionTolerance)
        {
            float taskTime = (Time.time - taskStartTime) * 1000f;
            float travelDistance = Mathf.Abs(value - taskStartValue);

            results.Add(new SliderTaskResult
            {
                task = tasksCompleted,
                slider_id = currentSlider,
                direction = targetIsMax ? "RIGHT" : "LEFT",
                start_value = taskStartValue,
                end_value = value,
                target_value = targetPosition,
                travel_distance = travelDistance,
                time_ms = taskTime,
                timestamp = testElapsedTime
            });

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>Task {tasksCompleted} komplett in {taskTime:F0}ms (Distanz: {travelDistance:F2})</color>");
            }

            waitingForTask = false;
        }
    }

    private void HighlightSlider(int sliderId, bool isMax)
    {
        GameObject slider = sliderId == 1 ? slider1Object : slider2Object;

        if (slider != null)
        {
            Renderer rend = slider.GetComponent<Renderer>();
            if (rend != null)
            {
                Color color = Color.yellow;  // Einheitliche Farbe - User weiss wohin
                rend.material.color = color;
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", color * 2f);
            }
        }
    }

    private void ResetSliderHighlights()
    {
        GameObject[] sliders = { slider1Object, slider2Object };

        foreach (GameObject slider in sliders)
        {
            if (slider != null)
            {
                Renderer rend = slider.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.white;
                    rend.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    private void UpdateSliderVisual(GameObject slider, float value, Vector3 minPos, Vector3 maxPos)
    {
        if (slider == null)
            return;

        // Value ist bereits 0.0 - 1.0
        // Interpoliere direkt zwischen Min und Max Position
        Vector3 targetPosition = Vector3.Lerp(minPos, maxPos, value);
        slider.transform.localPosition = targetPosition;
    }

    private void ExportToCSV()
    {
        if (results.Count == 0)
        {
            Debug.LogWarning("Keine Ergebnisse zum Exportieren");
            return;
        }

        try
        {
            bool fileExists = File.Exists(csvPath);

            using (StreamWriter writer = new StreamWriter(csvPath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Duration_sec,Task_Nr,Slider_ID,Direction,Start_Value,End_Value,Target_Value,Travel_Distance,Time_ms");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (var result in results)
                {
                    string line = $"{timestamp},{userId},{injuryLevel},{testDuration},{result.task},{result.slider_id},{result.direction},{result.start_value:F3},{result.end_value:F3},{result.target_value:F3},{result.travel_distance:F3},{result.time_ms:F2}";
                    writer.WriteLine(line);
                }
            }

            float avgTime = 0f;
            float avgDistance = 0f;

            foreach (var result in results)
            {
                avgTime += result.time_ms;
                avgDistance += result.travel_distance;
            }

            avgTime /= results.Count;
            avgDistance /= results.Count;

            Debug.Log($"<color=green>CSV gespeichert: {csvPath}</color>");
            Debug.Log($"  Tasks: {tasksCompleted}");
            Debug.Log($"  Avg Time: {avgTime:F0}ms");
            Debug.Log($"  Avg Distance: {avgDistance:F2}");

        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim CSV Export: {e.Message}");
        }
    }

    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 310, Screen.height - 220, 300, 210));
            GUILayout.Label("<b>Slider Test (Bis zum Anschlag)</b>");

            if (!testRunning)
            {
                GUILayout.Label($"Test Duration: {testDuration}s");

                if (GUILayout.Button("START SLIDER TEST"))
                {
                    StartSliderTest();
                }
            }
            else
            {
                float timeRemaining = testDuration - testElapsedTime;
                GUILayout.Label($"Zeit: {testElapsedTime:F1}s / {testDuration}s");
                GUILayout.Label($"Verbleibend: {timeRemaining:F1}s");
                GUILayout.Label($"Tasks: {tasksCompleted}");

                if (waitingForTask)
                {
                    string direction = targetIsMax ? "RECHTS" : "LINKS";
                    GUILayout.Label($"Task: Slider {currentSlider} -> {direction}");
                }

                GUILayout.Label($"Slider 1: {slider1Value:F2}");
                GUILayout.Label($"Slider 2: {slider2Value:F2}");

                if (GUILayout.Button("STOP (manuell)"))
                {
                    StopSliderTest();
                }
            }

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
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Set Slider 1 Min Position (Current)")]
    private void SetSlider1MinPosition()
    {
        if (slider1Object != null)
        {
            slider1MinUnityPosition = slider1Object.transform.localPosition;
            Debug.Log($"Slider 1 Min Position gesetzt: {slider1MinUnityPosition}");
        }
    }

    [ContextMenu("Set Slider 1 Max Position (Current)")]
    private void SetSlider1MaxPosition()
    {
        if (slider1Object != null)
        {
            slider1MaxUnityPosition = slider1Object.transform.localPosition;
            Debug.Log($"Slider 1 Max Position gesetzt: {slider1MaxUnityPosition}");
        }
    }

    [ContextMenu("Set Slider 2 Min Position (Current)")]
    private void SetSlider2MinPosition()
    {
        if (slider2Object != null)
        {
            slider2MinUnityPosition = slider2Object.transform.localPosition;
            Debug.Log($"Slider 2 Min Position gesetzt: {slider2MinUnityPosition}");
        }
    }

    [ContextMenu("Set Slider 2 Max Position (Current)")]
    private void SetSlider2MaxPosition()
    {
        if (slider2Object != null)
        {
            slider2MaxUnityPosition = slider2Object.transform.localPosition;
            Debug.Log($"Slider 2 Max Position gesetzt: {slider2MaxUnityPosition}");
        }
    }
#endif
}

[Serializable]
public class SliderTaskResult
{
    public int task;
    public int slider_id;
    public string direction;
    public float start_value;
    public float end_value;
    public float target_value;
    public float travel_distance;
    public float time_ms;
    public float timestamp;
}