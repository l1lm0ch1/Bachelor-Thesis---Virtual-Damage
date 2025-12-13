using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Button Manager für Reaktionstest
/// Highlighted zufälligen Button, misst Reaktionszeit
/// </summary>
public class ButtonManager : MonoBehaviour
{
    [Header("References")]
    public ArduinoUDPReceiver arduinoReceiver;

    [Header("Virtual Buttons (5 GameObjects in VR)")]
    public GameObject[] virtualButtons = new GameObject[5];

    [Header("Test Settings")]
    public int totalTrials = 30;
    public float delayBetweenTrials = 1.0f;

    [Header("Materials")]
    public Material buttonNormalMaterial;
    public Material buttonHighlightMaterial;

    [Header("CSV Export")]
    [Tooltip("Leer lassen für Default Path (Application.persistentDataPath)")]
    public string customCsvFolder = "";
    public string csvFileName = "reaction_test_results.csv";
    public string userId = "User_01";
    public int injuryLevel = 0;  // 0=keine, 1=leicht, 2=schwer

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Test State
    private bool testRunning = false;
    private int currentTrial = 0;
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

        // Button Pressed Event subscriben
        arduinoReceiver.ButtonPressed += OnButtonPressed;

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

        Debug.Log($"<color=green>Button Manager initialisiert</color>");
        Debug.Log($"  CSV Path: {csvPath}");

        // Alle Buttons auf Normal setzen
        ResetAllButtons();
    }

    void OnDestroy()
    {
        if (arduinoReceiver != null)
        {
            arduinoReceiver.ButtonPressed -= OnButtonPressed;
        }
    }

    void Update()
    {
        // TEMPORÄR: Keyboard Test (ohne Arduino)
        // Drücke Zahlen 1-5 zum Testen
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

        Debug.Log("<color=cyan>REAKTIONSTEST GESTARTET</color>");

        testRunning = true;
        currentTrial = 0;
        results.Clear();

        StartCoroutine(RunTest());
    }

    /// <summary>
    /// Test Coroutine
    /// </summary>
    private IEnumerator RunTest()
    {
        while (currentTrial < totalTrials)
        {
            // Trial starten
            currentTrial++;

            // Zufälligen Button wählen (1-5)
            targetButton = UnityEngine.Random.Range(1, 6);

            if (showDebugLogs)
            {
                Debug.Log($"Trial {currentTrial}/{totalTrials}: Button {targetButton}");
            }

            // Button highlighten
            HighlightButton(targetButton);

            // Timer starten
            trialStartTime = Time.time;
            waitingForInput = true;

            // Warte auf Input (oder Timeout)
            float timeout = 5.0f;  // 5 Sekunden Timeout
            float elapsed = 0f;

            while (waitingForInput && elapsed < timeout)
            {
                elapsed = Time.time - trialStartTime;
                yield return null;
            }

            // Timeout?
            if (waitingForInput)
            {
                Debug.LogWarning($"Trial {currentTrial}: TIMEOUT");

                results.Add(new TrialResult
                {
                    trial = currentTrial,
                    targetButton = targetButton,
                    pressedButton = -1,  // Timeout
                    reactionTimeMs = -1,
                    correct = false
                });

                waitingForInput = false;
            }

            // Button zurücksetzen
            ResetAllButtons();

            // Pause zwischen Trials
            yield return new WaitForSeconds(delayBetweenTrials);
        }

        // Test fertig
        testRunning = false;
        Debug.Log("<color=green>REAKTIONSTEST ABGESCHLOSSEN</color>");

        // Exportiere Ergebnisse
        ExportToCSV();
    }

    /// <summary>
    /// Button Press Callback
    /// </summary>
    private void OnButtonPressed(int buttonId, string action)
    {
        if (!waitingForInput)
            return;

        // Reaktionszeit berechnen
        float reactionTime = (Time.time - trialStartTime) * 1000f;  // in ms

        // Korrekt?
        bool correct = (buttonId == targetButton);

        if (showDebugLogs)
        {
            string status = correct ? "KORREKT" : "FALSCH";
            Debug.Log($"Button {buttonId} pressed: {reactionTime:F0}ms [{status}]");
        }

        // Ergebnis speichern
        results.Add(new TrialResult
        {
            trial = currentTrial,
            targetButton = targetButton,
            pressedButton = buttonId,
            reactionTimeMs = reactionTime,
            correct = correct
        });

        waitingForInput = false;
    }

    /// <summary>
    /// Highlighted einen Button
    /// </summary>
    private void HighlightButton(int buttonNumber)
    {
        int index = buttonNumber - 1;  // Array ist 0-indexed

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
    /// Setzt alle Buttons zurück
    /// </summary>
    private void ResetAllButtons()
    {
        for (int i = 0; i < virtualButtons.Length; i++)
        {
            if (virtualButtons[i] != null)
            {
                Renderer rend = virtualButtons[i].GetComponent<Renderer>();
                if (rend != null && buttonNormalMaterial != null)
                {
                    rend.material = buttonNormalMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Exportiert Ergebnisse zu CSV
    /// </summary>
    private void ExportToCSV()
    {
        try
        {
            // CSV Header
            bool fileExists = File.Exists(csvPath);

            using (StreamWriter writer = new StreamWriter(csvPath, true))  // append mode
            {
                // Header nur wenn Datei neu ist
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,User_ID,Injury_Level,Trial_Nr,Target_Button,Pressed_Button,Reaction_Time_ms,Correct");
                }

                // Daten schreiben
                foreach (var result in results)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string line = $"{timestamp},{userId},{injuryLevel},{result.trial},{result.targetButton},{result.pressedButton},{result.reactionTimeMs:F2},{result.correct}";
                    writer.WriteLine(line);
                }
            }

            Debug.Log($"<color=green>Ergebnisse gespeichert: {csvPath}</color>");
            Debug.Log($"   {results.Count} Trials exportiert");

            // Statistik
            int correct = 0;
            float totalTime = 0f;
            int validTrials = 0;

            foreach (var r in results)
            {
                if (r.correct) correct++;
                if (r.reactionTimeMs > 0)
                {
                    totalTime += r.reactionTimeMs;
                    validTrials++;
                }
            }

            float avgTime = validTrials > 0 ? totalTime / validTrials : 0;
            float accuracy = results.Count > 0 ? (correct / (float)results.Count) * 100f : 0;

            Debug.Log($"\nStatistik:");
            Debug.Log($"   Genauigkeit: {accuracy:F1}% ({correct}/{results.Count})");
            Debug.Log($"   Durchschnittliche Reaktionszeit: {avgTime:F0}ms");
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim CSV Export: {e.Message}");
        }
    }

    /// <summary>
    /// Zeigt Test-UI
    /// </summary>
    void OnGUI()
    {
        if (showDebugLogs)
        {
            GUILayout.BeginArea(new Rect(10, 320, 300, 150));
            GUILayout.Label("<b>Reaktionstest</b>");

            if (!testRunning)
            {
                if (GUILayout.Button("START TEST"))
                {
                    StartReactionTest();
                }
            }
            else
            {
                GUILayout.Label($"Trial: {currentTrial}/{totalTrials}");
                GUILayout.Label($"Target: Button {targetButton}");

                if (waitingForInput)
                {
                    float elapsed = Time.time - trialStartTime;
                    GUILayout.Label($"Zeit: {elapsed:F2}s");
                }
            }

            GUILayout.Label($"Results: {results.Count}");
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Öffnet den CSV Ordner im Explorer
    /// </summary>
    [ContextMenu("CSV Ordner öffnen")]
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
/// Trial Result Daten-Struktur
/// </summary>
[Serializable]
public class TrialResult
{
    public int trial;
    public int targetButton;
    public int pressedButton;
    public float reactionTimeMs;
    public bool correct;
}