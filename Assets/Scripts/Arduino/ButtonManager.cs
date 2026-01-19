using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Button Manager für Reaktionstest - ZEIT-BASIERT
/// Test läuft X Sekunden, so viele Trials wie möglich
/// </summary>
public class ButtonManager : MonoBehaviour
{
    [Header("References")]
    public ArduinoUDPReceiver arduinoReceiver;

    [Header("Virtual Buttons (5 GameObjects in VR)")]
    public GameObject[] virtualButtons = new GameObject[5];

    [Header("Test Settings")]
    [Tooltip("Test Dauer in Sekunden")]
    public float testDuration = 60f;

    [Tooltip("Delay zwischen Trials (Sekunden)")]
    public float delayBetweenTrials = 1.0f;

    [Tooltip("Timeout pro Trial (Sekunden)")]
    public float trialTimeout = 5.0f;

    [Header("Materials")]
    public Material buttonNormalMaterial;
    public Material buttonHighlightMaterial;

    [Header("CSV Export")]
    [Tooltip("Leer lassen fuer Default Path (Application.persistentDataPath)")]
    public string customCsvFolder = "C:\\Users\\lilli\\Documents\\CSV Files\\Testing";
    public string csvFileName = "reaction_test_results.csv";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // User Info (aus AdminInfoPanel)
    private string userId = "Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Button_Physical";

    // Test State
    private bool testRunning = false;
    private float testStartTime = 0f;
    private float testElapsedTime = 0f;
    private int trialsCompleted = 0;
    private int targetButton = 0;
    private float trialStartTime = 0f;
    private bool waitingForInput = false;

    // Results
    private List<TrialResult> results = new List<TrialResult>();

    // CSV Path
    private string csvPath;

    void Start()
    {
        // Arduino Receiver finden falls nicht zugewiesen
        if (arduinoReceiver == null)
        {
            arduinoReceiver = FindFirstObjectByType<ArduinoUDPReceiver>();
        }

        if (arduinoReceiver == null)
        {
            Debug.LogError("ArduinoUDPReceiver nicht gefunden!");
            return;
        }

        // Button Events subscriben
        arduinoReceiver.ButtonPressed += OnButtonPressed;
        arduinoReceiver.ButtonReleased += OnButtonReleased;

        // CSV Path bestimmen
        if (string.IsNullOrEmpty(customCsvFolder))
        {
            // Default: Application.persistentDataPath
            csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
        }
        else
        {
            // Custom Folder
            csvPath = Path.Combine(customCsvFolder, csvFileName);

            // Erstelle Ordner falls nicht vorhanden
            if (!Directory.Exists(customCsvFolder))
            {
                try
                {
                    Directory.CreateDirectory(customCsvFolder);
                    Debug.Log($"CSV Ordner erstellt: {customCsvFolder}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Kann CSV Ordner nicht erstellen: {e.Message}");
                    // Fallback zu Default
                    csvPath = Path.Combine(Application.persistentDataPath, csvFileName);
                }
            }
        }

        Debug.Log("<color=green>Button Manager initialisiert (ZEIT-BASIERT)</color>");
        Debug.Log($"  CSV Path: {csvPath}");
        Debug.Log($"  Test Duration: {testDuration}s");

        // Alle Buttons auf Normal setzen
        ResetAllButtons();
    }

    void OnDestroy()
    {
        if (arduinoReceiver != null)
        {
            arduinoReceiver.ButtonPressed -= OnButtonPressed;
            arduinoReceiver.ButtonReleased -= OnButtonReleased;
        }
    }

    void Update()
    {
        // Update elapsed time waehrend Test
        if (testRunning)
        {
            testElapsedTime = Time.time - testStartTime;
        }

        // Keyboard Test - Button Press
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            OnButtonPressed(1, "pressed");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            OnButtonPressed(2, "pressed");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            OnButtonPressed(3, "pressed");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            OnButtonPressed(4, "pressed");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            OnButtonPressed(5, "pressed");
        }

        // Keyboard Test - Button Release (fuer Preview Mode)
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            OnButtonPressed(1, "released");
        }
        else if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            OnButtonPressed(2, "released");
        }
        else if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            OnButtonPressed(3, "released");
        }
        else if (Input.GetKeyUp(KeyCode.Alpha4))
        {
            OnButtonPressed(4, "released");
        }
        else if (Input.GetKeyUp(KeyCode.Alpha5))
        {
            OnButtonPressed(5, "released");
        }
    }

    /// <summary>
    /// Startet Reaktionstest
    /// </summary>
    public void StartReactionTest()
    {
        if (testRunning)
        {
            Debug.LogWarning("Test läuft bereits!");
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

        Debug.Log($"<color=cyan>REAKTIONSTEST GESTARTET ({testDuration}s)</color>");
        Debug.Log($"  User: {userId}");
        Debug.Log($"  Injury: {injuryLevel}");

        testRunning = true;
        testStartTime = Time.time;
        testElapsedTime = 0f;
        trialsCompleted = 0;
        results.Clear();

        StartCoroutine(RunTest());
    }

    /// <summary>
    /// Stoppt Test manuell
    /// </summary>
    public void StopReactionTest()
    {
        if (!testRunning)
            return;

        StopAllCoroutines();
        testRunning = false;
        waitingForInput = false;

        Debug.Log("<color=yellow>REAKTIONSTEST MANUELL BEENDET</color>");

        ResetAllButtons();
        ExportToCSV();
    }

    /// <summary>
    /// Test Coroutine - ZEIT-BASIERT
    /// </summary>
    private IEnumerator RunTest()
    {
        while (testElapsedTime < testDuration)
        {
            // Check: Genug Zeit für weiteres Trial?
            float timeRemaining = testDuration - testElapsedTime;
            if (timeRemaining < (trialTimeout + delayBetweenTrials))
            {
                // Nicht genug Zeit für komplettes Trial
                Debug.Log("Nicht genug Zeit für weiteres Trial");
                break;
            }

            // Trial starten
            trialsCompleted++;

            // Zufälligen Button wählen (1-5)
            targetButton = UnityEngine.Random.Range(1, 6);

            if (showDebugLogs)
            {
                Debug.Log($"Trial {trialsCompleted}: Button {targetButton} (Zeit: {testElapsedTime:F1}s/{testDuration}s)");
            }

            // Button highlighten
            HighlightButton(targetButton);

            // Timer starten
            trialStartTime = Time.time;
            waitingForInput = true;

            // Warte auf Input (oder Timeout)
            float elapsed = 0f;

            while (waitingForInput && elapsed < trialTimeout)
            {
                elapsed = Time.time - trialStartTime;

                // Update elapsed time
                testElapsedTime = Time.time - testStartTime;

                // Check: Gesamtzeit abgelaufen?
                if (testElapsedTime >= testDuration)
                {
                    Debug.Log("Test Zeit abgelaufen während Trial");
                    waitingForInput = false;
                    break;
                }

                yield return null;
            }

            // Timeout?
            if (waitingForInput)
            {
                Debug.LogWarning($"Trial {trialsCompleted}: TIMEOUT");

                results.Add(new TrialResult
                {
                    trial = trialsCompleted,
                    targetButton = targetButton,
                    pressedButton = -1,  // Timeout
                    reactionTimeMs = -1,
                    correct = false,
                    timestamp = testElapsedTime
                });

                waitingForInput = false;
            }

            // Button zurücksetzen
            ResetAllButtons();

            // Update elapsed time
            testElapsedTime = Time.time - testStartTime;

            // Check: Zeit für Delay?
            if (testElapsedTime + delayBetweenTrials < testDuration)
            {
                // Pause zwischen Trials
                yield return new WaitForSeconds(delayBetweenTrials);
            }
            else
            {
                // Keine Zeit mehr für Delay
                break;
            }

            // Update elapsed time nach Delay
            testElapsedTime = Time.time - testStartTime;
        }

        // Test fertig
        testRunning = false;
        float finalTime = Time.time - testStartTime;

        Debug.Log("<color=green>REAKTIONSTEST ABGESCHLOSSEN</color>");
        Debug.Log($"  Dauer: {finalTime:F1}s");
        Debug.Log($"  Trials: {trialsCompleted}");

        // Exportiere Ergebnisse
        ExportToCSV();
    }

    /// <summary>
    /// Button Press Callback - PREVIEW MODE + TEST MODE
    /// </summary>
    private void OnButtonPressed(int buttonId, string action)
    {
        // PREVIEW MODE: Visuelles Feedback NUR ausserhalb vom Test
        if (!testRunning)
        {
            if (action == "pressed")
            {
                HighlightButton(buttonId);

                if (showDebugLogs)
                {
                    Debug.Log($"[PREVIEW] Button {buttonId} pressed");
                }
            }
            else if (action == "released")
            {
                ResetButton(buttonId);
            }
        }

        // TEST MODE: Recording nur waehrend Test
        if (!waitingForInput)
            return;

        // Reaktionszeit berechnen
        float reactionTime = (Time.time - trialStartTime) * 1000f;

        // Korrekt?
        bool correct = (buttonId == targetButton);

        if (showDebugLogs)
        {
            string status = correct ? "KORREKT" : "FALSCH";
            Debug.Log($"[TEST] Button {buttonId} pressed: {reactionTime:F0}ms [{status}]");
        }

        // Ergebnis speichern
        results.Add(new TrialResult
        {
            trial = trialsCompleted,
            targetButton = targetButton,
            pressedButton = buttonId,
            reactionTimeMs = reactionTime,
            correct = correct,
            timestamp = testElapsedTime
        });

        // Stop waiting
        waitingForInput = false;
    }

    /// <summary>
    /// Button Released Callback - wird von Arduino Event aufgerufen
    /// </summary>
    private void OnButtonReleased(int buttonId, string action)
    {
        // Einfach an OnButtonPressed weiterleiten mit "released" action
        OnButtonPressed(buttonId, "released");
    }

    /// <summary>
    /// Highlighted Button
    /// </summary>
    private void HighlightButton(int buttonNumber)
    {
        int index = buttonNumber - 1;

        if (index >= 0 && index < virtualButtons.Length && virtualButtons[index] != null)
        {
            Renderer rend = virtualButtons[index].GetComponent<Renderer>();
            if (rend != null && buttonHighlightMaterial != null)
            {
                rend.material = buttonHighlightMaterial;
            }
        }
    }

    /// <summary>
    /// Reset einzelnen Button (fuer Preview Mode)
    /// </summary>
    private void ResetButton(int buttonNumber)
    {
        int index = buttonNumber - 1;

        if (index >= 0 && index < virtualButtons.Length && virtualButtons[index] != null)
        {
            Renderer rend = virtualButtons[index].GetComponent<Renderer>();
            if (rend != null && buttonNormalMaterial != null)
            {
                rend.material = buttonNormalMaterial;
            }
        }
    }

    /// <summary>
    /// Reset alle Buttons zu Normal
    /// </summary>
    private void ResetAllButtons()
    {
        if (buttonNormalMaterial == null)
            return;

        foreach (GameObject btn in virtualButtons)
        {
            if (btn != null)
            {
                Renderer rend = btn.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = buttonNormalMaterial;
                }
            }
        }
    }

    /// <summary>
    /// CSV Export - Nur eine File mit allen Trial Events
    /// </summary>
    private void ExportToCSV()
    {
        CSVWriter writer = new CSVWriter(customCsvFolder, showDebugLogs);

        if (AdminInfoPanel.Instance != null)
        {
            AdminInfoPanel.Instance.SetCSVPath(writer.FolderPath);
        }

        List<(int, int, int, float, bool, float)> data = new List<(int, int, int, float, bool, float)>();
        foreach (var result in results)
        {
            data.Add((result.trial, result.targetButton, result.pressedButton, result.reactionTimeMs, result.correct, result.timestamp));
        }

        writer.WriteButtonTestCSV(userId, injuryLevel, testType, testDuration, data, csvFileName);
    }

    /// <summary>
    /// OnGUI - Debug Status Display (START wird von AdminUI gehandled)
    /// </summary>
    void OnGUI()
    {
        if (showDebugLogs)
        {
            // Position RECHTS (SortingTaskManager ist links)
            GUILayout.BeginArea(new Rect(Screen.width - 810, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>BUTTON TEST (Physical)</b>");
            GUILayout.Space(5);

            // User Info aus AdminInfoPanel anzeigen
            if (AdminInfoPanel.Instance != null)
            {
                GUILayout.Label($"User: {AdminInfoPanel.Instance.GetUserName()}");
                GUILayout.Label($"Injury: {AdminInfoPanel.Instance.GetInjuryState()}");
                GUILayout.Space(5);
            }

            if (!testRunning)
            {
                GUILayout.Label($"Duration: {testDuration}s");

                if (GUILayout.Button("START BUTTON TEST", GUILayout.Height(40)))
                {
                    StartReactionTest();
                }
            }
            else
            {
                float timeRemaining = testDuration - testElapsedTime;
                GUILayout.Label($"Zeit: {testElapsedTime:F1}s / {testDuration}s");
                GUILayout.Label($"Verbleibend: {timeRemaining:F1}s");
                GUILayout.Label($"Trials: {trialsCompleted}");

                int correctCount = 0;
                foreach (var r in results)
                {
                    if (r.correct) correctCount++;
                }

                float accuracy = results.Count > 0 ? (float)correctCount / results.Count * 100f : 0f;
                GUILayout.Label($"Korrekt: {correctCount}/{results.Count} ({accuracy:F0}%)");

                if (results.Count > 0)
                {
                    var lastResult = results[results.Count - 1];
                    if (lastResult.reactionTimeMs > 0)
                    {
                        GUILayout.Label($"Letzte RT: {lastResult.reactionTimeMs:F0}ms");
                    }
                }

                if (GUILayout.Button("STOP (manuell)"))
                {
                    StopReactionTest();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Trial Result Data
    /// </summary>
    [Serializable]
    public class TrialResult
    {
        public int trial;
        public int targetButton;
        public int pressedButton;
        public float reactionTimeMs;
        public bool correct;
        public float timestamp;  // Zeit seit Test Start
    }
}