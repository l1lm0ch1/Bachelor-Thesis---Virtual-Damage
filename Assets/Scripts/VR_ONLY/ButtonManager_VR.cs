using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager_VR : MonoBehaviour
{
    [Header("VR Buttons")]
    public GameObject[] vrButtons = new GameObject[5];

    [Header("Button Materials")]
    public Material buttonNormalMaterial;
    public Material buttonHighlightMaterial;

    [Header("Test Settings")]
    public float testDuration = 60f;
    public float trialTimeout = 5f;
    public float delayBetweenTrials = 0.5f;

    [Header("CSV Export")]
    public string csvFileName = "button_vr_test_results.csv";
    public string customCsvFolder = "";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string userId = "VR_Participant_001";
    private string injuryLevel = "Normal";
    private string testType = "Button_VR";

    private bool testRunning = false;
    private bool waitingForInput = false;
    private int targetButton = -1;
    private float testStartTime;
    private float testElapsedTime;
    private float trialStartTime;
    private int trialsCompleted = 0;

    private List<TrialResult> results = new List<TrialResult>();

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
        VRButton.OnButtonInteraction += OnButtonInteraction;

        Debug.Log("<color=green>Button Manager VR (HOVER) initialisiert</color>");

        ResetAllButtons();
    }

    void OnDestroy()
    {
        VRButton.OnButtonInteraction -= OnButtonInteraction;
    }

    void Update()
    {
        if (testRunning)
        {
            testElapsedTime = Time.time - testStartTime;
        }
    }

    public void StartReactionTest()
    {
        if (testRunning)
        {
            Debug.LogWarning("Test läuft bereits!");
            return;
        }

        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
            testType = AdminInfoPanel.Instance.GetTestType();
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

    private IEnumerator RunTest()
    {
        while (testElapsedTime < testDuration)
        {
            float timeRemaining = testDuration - testElapsedTime;
            if (timeRemaining < (trialTimeout + delayBetweenTrials))
            {
                Debug.Log("Nicht genug Zeit für weiteres Trial");
                break;
            }

            trialsCompleted++;
            targetButton = UnityEngine.Random.Range(1, 6);

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>Trial {trialsCompleted}: Button {targetButton}</color>");
            }

            HighlightButton(targetButton);

            trialStartTime = Time.time;
            waitingForInput = true;

            float elapsed = 0f;

            while (waitingForInput && elapsed < trialTimeout)
            {
                elapsed = Time.time - trialStartTime;
                testElapsedTime = Time.time - testStartTime;

                if (testElapsedTime >= testDuration)
                {
                    Debug.Log("Test Zeit abgelaufen während Trial");
                    waitingForInput = false;
                    break;
                }

                yield return null;
            }

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

            ResetAllButtons();

            yield return new WaitForSeconds(delayBetweenTrials);

            testElapsedTime = Time.time - testStartTime;
        }

        testRunning = false;
        float finalTime = Time.time - testStartTime;

        Debug.Log("<color=green>VR REAKTIONSTEST ABGESCHLOSSEN</color>");
        Debug.Log($"  Dauer: {finalTime:F1}s");
        Debug.Log($"  Trials: {trialsCompleted}");

        ExportToCSV();
    }

    private void OnButtonInteraction(int buttonId, string action)
    {
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

        if (!waitingForInput || action != "pressed")
            return;

        float reactionTime = (Time.time - trialStartTime) * 1000f;

        bool correct = (buttonId == targetButton);

        if (showDebugLogs)
        {
            string status = correct ? "<color=green>KORREKT ✓</color>" : "<color=red>FALSCH ✗</color>";
            Debug.Log($"<color=cyan>[TEST] Button {buttonId} pressed</color>");
            Debug.Log($"<color=cyan>  Reaktionszeit: {reactionTime:F0}ms</color>");
            Debug.Log($"<color=cyan>  Status: {status}</color>");
        }

        results.Add(new TrialResult
        {
            trial = trialsCompleted,
            targetButton = targetButton,
            pressedButton = buttonId,
            reactionTimeMs = reactionTime,
            correct = correct,
            timestamp = testElapsedTime
        });

        waitingForInput = false;
    }

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

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>VR BUTTON TEST (HOVER)</b>");
        GUILayout.Space(10);

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