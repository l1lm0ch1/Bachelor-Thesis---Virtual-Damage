using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// BUTTON MANAGER VR - PURE VR VERSION
/// ====================================
/// Reaktionstest mit VR Buttons (ohne Arduino/Physical Objects)
/// Zeit-basierter Test mit zufaelligen Button-Tasks
/// </summary>
public class ButtonManager_VR : MonoBehaviour
{
    [Header("VR Buttons")]
    [Tooltip("Array mit allen 5 VR Button GameObjects")]
    public GameObject[] vrButtons = new GameObject[5];

    [Header("Button Materials")]
    [Tooltip("Material fuer normalen Button State")]
    public Material buttonNormalMaterial;

    [Tooltip("Material fuer Target Button (gruen)")]
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
    public string customCsvFolder = "C:\\Users\\lilli\\OneDrive\\FH\\5. Semester\\BachelorArbeit\\ReactionTest_UserData\\TESTING LILLI";

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
        string csvFilename = csvFileName; // Default Fallback

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

        Debug.Log("<color=green>Button Manager VR initialisiert</color>");
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
        // Update elapsed time waehrend Test
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
            Debug.LogWarning("Test laeuft bereits!");
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

        Debug.Log($"<color=cyan>VR REAKTIONSTEST GESTARTET ({testDuration}s)</color>");
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
            // Check: Genug Zeit fuer weiteres Trial?
            float timeRemaining = testDuration - testElapsedTime;
            if (timeRemaining < (trialTimeout + delayBetweenTrials))
            {
                Debug.Log("Nicht genug Zeit fuer weiteres Trial");
                break;
            }

            // Trial starten
            trialsCompleted++;

            // Zufaelligen Button waehlen (1-5)
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
                    Debug.Log("Test Zeit abgelaufen waehrend Trial");
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

            // Alle Buttons zuruecksetzen
            ResetAllButtons();

            // Delay vor naechstem Trial
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
        if (!waitingForInput || action != "pressed")
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
        try
        {
            using (StreamWriter writer = new StreamWriter(csvPath))
            {
                // Header
                writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Test_Duration_sec,Trial_Nr,Target_Button,Pressed_Button,Reaction_Time_ms,Correct");

                // Daten
                foreach (var result in results)
                {
                    string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        userId,
                        injuryLevel,
                        testType,
                        testDuration,
                        result.trial,
                        result.targetButton,
                        result.pressedButton,
                        result.reactionTimeMs.ToString("F2"),
                        result.correct ? "TRUE" : "FALSE"
                    );

                    writer.WriteLine(line);
                }
            }

            Debug.Log($"<color=green>CSV exportiert: {csvPath}</color>");
            Debug.Log($"  Trials gespeichert: {results.Count}");

            // Statistik
            int correctCount = 0;
            float totalReactionTime = 0f;
            int validTrials = 0;

            foreach (var result in results)
            {
                if (result.correct)
                    correctCount++;

                if (result.reactionTimeMs > 0)
                {
                    totalReactionTime += result.reactionTimeMs;
                    validTrials++;
                }
            }

            float accuracy = results.Count > 0 ? (correctCount / (float)results.Count) * 100f : 0f;
            float avgReactionTime = validTrials > 0 ? totalReactionTime / validTrials : 0f;

            Debug.Log($"  Accuracy: {accuracy:F1}%");
            Debug.Log($"  Avg Reaction Time: {avgReactionTime:F0}ms");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV Export Fehler: {e.Message}");
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>VR BUTTON TEST</b>");
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