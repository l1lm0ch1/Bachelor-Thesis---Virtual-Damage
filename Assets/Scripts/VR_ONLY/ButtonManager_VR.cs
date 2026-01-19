using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// BUTTON MANAGER VR - HOVER-BASIERTE VERSION
/// ==========================================
/// Reaktionstest mit VR Buttons (Hover-Interaktion)
/// Zeit-basierter Test mit zufälligen Button-Tasks
/// </summary>
public class ButtonManager_VR : MonoBehaviour
{
    [Header("VR Buttons")]
    [Tooltip("Array mit allen 5 VR Button GameObjects")]
    public GameObject[] vrButtons = new GameObject[5];

    [Header("Button Materials")]
    [Tooltip("Material für normalen Button State")]
    public Material buttonNormalMaterial;

    [Tooltip("Material für Target Button (grün)")]
    public Material buttonHighlightMaterial;

    [Header("Test Settings")]
    [Tooltip("Gesamte Test-Dauer in Sekunden")]
    public float testDuration = 60f;

    [Tooltip("Maximale Zeit pro Trial in Sekunden")]
    public float trialTimeout = 5f;

    [Tooltip("Delay zwischen Trials in Sekunden")]
    public float delayBetweenTrials = 0.5f;

    [Header("CSV Export")]
    [Tooltip("CSV Dateiname")]
    public string csvFileName = "button_vr_test_results.csv";

    [Tooltip("Custom CSV Ordner (leer = Application.persistentDataPath)")]
    public string customCsvFolder = "C:\\Users\\lilli\\Documents\\CSV Files\\Testing";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // User Info (aus AdminInfoPanel)
    private string userId = "VR_Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Button_VR";

    // Test State
    private bool testRunning = false;
    private bool waitingForInput = false;
    private int targetButton = -1;
    private float testStartTime;
    private float testElapsedTime;
    private float trialStartTime;
    private int trialsCompleted = 0;

    // Results
    private List<TrialResult> results = new List<TrialResult>();
    private string csvPath;

    [System.Serializable]
    private class TrialResult
    {
        public int trial;
        public int targetButton;
        public int pressedButton;
        public float reactionTimeMs;
        public bool correct;
        public float timestamp;
    }

    void Start()
    {
        // VRButton Events subscriben
        VRButton.OnButtonInteraction += OnButtonInteraction;

        // CSV Path bestimmen
        string csvFilename = csvFileName;

        // Hole CSV Filename vom AdminInfoPanel
        if (AdminInfoPanel.Instance != null)
        {
            csvFilename = AdminInfoPanel.Instance.GetReactionTestCSVFilename();
        }

        if (string.IsNullOrEmpty(customCsvFolder))
        {
            csvPath = Path.Combine(Application.persistentDataPath, csvFilename);
        }
        else
        {
            csvPath = Path.Combine(customCsvFolder, csvFilename);

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
                    csvPath = Path.Combine(Application.persistentDataPath, csvFilename);
                }
            }
        }

        Debug.Log("<color=green>Button Manager VR (HOVER) initialisiert</color>");
        Debug.Log($"  CSV Path: {csvPath}");
        Debug.Log($"  Test Duration: {testDuration}s");

        // Alle Buttons auf Normal setzen
        ResetAllButtons();
    }

    void OnDestroy()
    {
        // Events unsubscriben
        VRButton.OnButtonInteraction -= OnButtonInteraction;
    }

    void Update()
    {
        // Update elapsed time während Test
        if (testRunning)
        {
            testElapsedTime = Time.time - testStartTime;
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

        Debug.Log($"<color=cyan>VR REAKTIONSTEST GESTARTET (HOVER) ({testDuration}s)</color>");
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

        Debug.Log("<color=yellow>VR REAKTIONSTEST MANUELL BEENDET</color>");

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
                Debug.Log("Nicht genug Zeit für weiteres Trial");
                break;
            }

            // Trial starten
            trialsCompleted++;

            // Zufälligen Button wählen (1-5)
            targetButton = UnityEngine.Random.Range(1, 6);

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>═══════════════════════════════════</color>");
                Debug.Log($"<color=cyan>Trial {trialsCompleted}: Button {targetButton}</color>");
                Debug.Log($"<color=cyan>Zeit: {testElapsedTime:F1}s / {testDuration}s</color>");
                Debug.Log($"<color=cyan>═══════════════════════════════════</color>");
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
                    pressedButton = -1,
                    reactionTimeMs = -1,
                    correct = false,
                    timestamp = testElapsedTime
                });

                waitingForInput = false;
            }

            // Alle Buttons zurücksetzen
            ResetAllButtons();

            // Delay vor nächstem Trial
            yield return new WaitForSeconds(delayBetweenTrials);

            // Update elapsed time nach Delay
            testElapsedTime = Time.time - testStartTime;
        }

        // Test fertig
        testRunning = false;
        float finalTime = Time.time - testStartTime;

        Debug.Log("<color=green>VR REAKTIONSTEST ABGESCHLOSSEN</color>");
        Debug.Log($"  Dauer: {finalTime:F1}s");
        Debug.Log($"  Trials: {trialsCompleted}");

        // Exportiere Ergebnisse
        ExportToCSV();
    }

    /// <summary>
    /// VRButton Interaction Callback
    /// </summary>
    private void OnButtonInteraction(int buttonId, string action)
    {
        // PREVIEW MODE: Visuelles Feedback NUR außerhalb vom Test
        if (!testRunning)
        {
            if (action == "pressed")
            {
                HighlightButton(buttonId);

                if (showDebugLogs)
                {
                    Debug.Log($"<color=magenta>[PREVIEW] Button {buttonId} pressed</color>");
                }
            }
            else if (action == "released")
            {
                ResetButton(buttonId);
            }
            return;
        }

        // TEST MODE: Recording nur während Test
        if (!waitingForInput || action != "pressed")
            return;

        // Reaktionszeit berechnen
        float reactionTime = (Time.time - trialStartTime) * 1000f;

        // Korrekt?
        bool correct = (buttonId == targetButton);

        if (showDebugLogs)
        {
            string status = correct ? "<color=green>KORREKT ✓</color>" : "<color=red>FALSCH ✗</color>";
            Debug.Log($"<color=cyan>══════════════════════════════════════════</color>");
            Debug.Log($"<color=cyan>[TEST] Button {buttonId} pressed</color>");
            Debug.Log($"<color=cyan>  Reaktionszeit: {reactionTime:F0}ms</color>");
            Debug.Log($"<color=cyan>  Status: {status}</color>");
            Debug.Log($"<color=cyan>══════════════════════════════════════════</color>");
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
    /// Highlighted Button
    /// </summary>
    private void HighlightButton(int buttonNumber)
    {
        int index = buttonNumber - 1;

        if (index >= 0 && index < vrButtons.Length && vrButtons[index] != null)
        {
            VRButton vrButton = vrButtons[index].GetComponent<VRButton>();
            if (vrButton != null && buttonHighlightMaterial != null)
            {
                vrButton.SetMaterial(buttonHighlightMaterial);
            }
        }
    }

    /// <summary>
    /// Reset einzelnen Button
    /// </summary>
    private void ResetButton(int buttonNumber)
    {
        int index = buttonNumber - 1;

        if (index >= 0 && index < vrButtons.Length && vrButtons[index] != null)
        {
            VRButton vrButton = vrButtons[index].GetComponent<VRButton>();
            if (vrButton != null)
            {
                vrButton.ResetMaterial();
            }
        }
    }

    /// <summary>
    /// Reset alle Buttons
    /// </summary>
    private void ResetAllButtons()
    {
        for (int i = 0; i < vrButtons.Length; i++)
        {
            if (vrButtons[i] != null)
            {
                VRButton vrButton = vrButtons[i].GetComponent<VRButton>();
                if (vrButton != null)
                {
                    vrButton.ResetMaterial();
                }
            }
        }
    }

    /// <summary>
    /// Exportiere Results als CSV
    /// </summary>
    private void ExportToCSV()
    {
        CSVWriter writer = new CSVWriter(customCsvFolder, showDebugLogs);

        // Konvertiere results in simple Tuples
        List<(int, int, int, float, bool, float)> data = new List<(int, int, int, float, bool, float)>();
        foreach (var result in results)
        {
            data.Add((result.trial, result.targetButton, result.pressedButton, result.reactionTimeMs, result.correct, result.timestamp));
        }

        // CSV Writer übernimmt ALLES (inklusive AdminInfoPanel Filename!)
        writer.WriteButtonTestCSV(userId, injuryLevel, testType, testDuration, data, csvFileName);
    }

    void OnGUI()
    {
        // Position von Physical Button Manager
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>VR BUTTON TEST (HOVER)</b>");
        GUILayout.Space(10);

        // User Info aus AdminInfoPanel anzeigen
        if (AdminInfoPanel.Instance != null)
        {
            GUILayout.Label($"User: {AdminInfoPanel.Instance.GetUserName()}");
            GUILayout.Label($"Injury: {AdminInfoPanel.Instance.GetInjuryState()}");
            GUILayout.Space(10);
        }

        if (!testRunning)
        {
            GUILayout.Label($"Duration: {testDuration}s");

            if (GUILayout.Button("START BUTTON TEST", GUILayout.Height(50)))
            {
                StartReactionTest();
            }
        }
        else
        {
            GUILayout.Label($"Zeit: {testElapsedTime:F1}s / {testDuration}s");
            GUILayout.Label($"Trials: {trialsCompleted}");

            if (waitingForInput)
            {
                GUILayout.Label($"<color=green>Target: Button {targetButton}</color>");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("STOP TEST", GUILayout.Height(40)))
            {
                StopReactionTest();
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}